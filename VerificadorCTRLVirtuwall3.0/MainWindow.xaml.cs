using DnsClient;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VerificadorCTRLVirtuwall 
{
    public partial class MainWindow : Window
    {
        // Cores
        private readonly Brush corAcento;
        private readonly Brush corOK;
        private readonly Brush corFALHA;
        private readonly Brush corAVISO;
        private readonly Brush corLog;
        private readonly Brush corTexto;
        private readonly Brush statusOK;
        private readonly Brush statusFalha;
        private readonly Brush statusAguarde;
        private readonly Brush corTextoOK;
        private readonly Brush corTextoFalha;
        private readonly Brush corTextoAguarde;
        private readonly Brush corIconeOK = Brushes.Green;
        private readonly Brush corIconeFalha = Brushes.Red;
        private readonly Brush corIconeAviso = Brushes.DarkGoldenrod;
        private enum LogStatus { Info, Titulo, OK, Falha, Aviso }

        public MainWindow()
        {
            InitializeComponent();
            corAcento = (Brush)FindResource("VirtuwallGreenBrush");
            statusOK = (Brush)FindResource("StatusGreenBrush");
            statusFalha = (Brush)FindResource("StatusRedBrush");
            statusAguarde = (Brush)FindResource("StatusGrayBrush");
            corOK = Brushes.Green;
            corFALHA = Brushes.Red;
            corAVISO = Brushes.DarkGoldenrod;
            corLog = Brushes.Gray;
            corTexto = Brushes.Black;
            corTextoOK = (Brush)new BrushConverter().ConvertFrom("#2E7D32")!;
            corTextoFalha = (Brush)new BrushConverter().ConvertFrom("#C62828")!;
            corTextoAguarde = (Brush)new BrushConverter().ConvertFrom("#37474F")!;
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(n => new AdapterItem { DisplayName = n.Name, Interface = n })
                    .ToList();
                adapterComboBox.ItemsSource = adapters;
                adapterComboBox.DisplayMemberPath = "DisplayName";
                if (adapterComboBox.Items.Count > 0)
                {
                    adapterComboBox.SelectedIndex = 0;
                }
                else
                {
                    adapterComboBox.Items.Add("Nenhum adaptador ativo encontrado.");
                    adapterComboBox.IsEnabled = false;
                    startButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                Log($"Erro ao carregar adaptadores: {ex.Message}", LogStatus.Falha);
            }
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            logRichTextBox.Document.Blocks.Clear();
            StatusCard.Visibility = Visibility.Collapsed;
        }

        private async void OnStartButtonClick(object sender, RoutedEventArgs e)
        {
            logRichTextBox.Document.Blocks.Clear();
            startButton.IsEnabled = false;
            startButton.Content = "Aguarde...";
            SetStatusCard(LogStatus.Info, "Executando testes... Por favor, aguarde.");
            var fqdn = fqdnTextBox.Text;
            var domain = domainTextBox.Text;
            var srvName = $"_barcomanagement._tcp.{domain}";
            int.TryParse(portTextBox.Text, out int srvPort);
            var selectedAdapterItem = adapterComboBox.SelectedItem as AdapterItem;
            if (selectedAdapterItem == null)
            {
                Log("Nenhum adaptador de rede selecionado.", LogStatus.Falha);
                SetStatusCard(LogStatus.Falha, "REPROVADO");
                startButton.IsEnabled = true;
                startButton.Content = "Iniciar Verificacao";
                return;
            }
            var adapter = selectedAdapterItem.Interface;
            var adapterIp = adapter.GetIPProperties().UnicastAddresses
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
            if (adapterIp == null)
            {
                Log($"Adaptador selecionado ({adapter.Name}) nao tem um endereco IPv4.", LogStatus.Falha);
                SetStatusCard(LogStatus.Falha, "REPROVADO");
                startButton.IsEnabled = true;
                startButton.Content = "Iniciar Verificacao";
                return;
            }

            Log($"--- Iniciando Verificacao na Interface {adapter.Name} ({adapterIp}) ---", LogStatus.Titulo);
            bool overallStatus = true;
            try
            {
                Log("[25] Verificando DHCP (Opcoes 6, 15, 42)...", LogStatus.Titulo);
                var dhcpClient = new DhcpClient(adapterIp);
                var dhcpOptions = await dhcpClient.RequestDhcpOptionsAsync(adapter.GetPhysicalAddress());
                if (dhcpOptions == null)
                {
                    Log("Nao foi possivel obter resposta do servidor DHCP (Timeout).", LogStatus.Falha);
                    overallStatus = false;
                }
                else
                {
                    var dnsServer = dhcpOptions.DnsServers.FirstOrDefault();
                    if (dnsServer != null)
                    {
                        Log($"[Opcao 6] Servidor DNS encontrado: {dnsServer}", LogStatus.OK);
                        overallStatus &= await TestDnsAsync(dnsServer, fqdn, srvName, srvPort);
                    }
                    else
                    {
                        Log("[Opcao 6] Servidor DHCP nao entregou um servidor DNS.", LogStatus.Falha);
                        overallStatus = false;
                    }
                    if (dhcpOptions.DomainName == domain)
                    {
                        Log($"[Opcao 15] Sufixo DNS encontrado e CORRETO: {dhcpOptions.DomainName}", LogStatus.OK);
                    }
                    else
                    {
                        Log($"[Opcao 15] Sufixo DNS incorreto. Esperado: '{domain}', Recebido: '{dhcpOptions.DomainName}'", LogStatus.Falha);
                        overallStatus = false;
                    }
                    var ntpServer = dhcpOptions.NtpServers.FirstOrDefault();
                    if (ntpServer != null)
                    {
                        Log($"[Opcao 42] Servidor NTP encontrado: {ntpServer}", LogStatus.OK);
                        overallStatus &= await TestNtpAsync(ntpServer);
                    }
                    else
                    {
                        Log("[Opcao 42] Servidor DHCP nao entregou um servidor NTP.", LogStatus.Falha);
                        overallStatus = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"ERRO CRITICO DURANTE O TESTE DHCP: {ex.GetType().Name} - {ex.Message}", LogStatus.Falha);
                Log("NOTA: Este teste (DHCPINFORM) requer privilegios de Administrador para funcionar.", LogStatus.Aviso);
                overallStatus = false;
            }
            Log("--- Verificacao Concluida ---", LogStatus.Titulo);
            if (overallStatus)
            {
                SetStatusCard(LogStatus.OK, "APROVADO");
                Log("A infraestrutura parece estar pronta para a implantacao do Barco CTRL.", LogStatus.OK);
            }
            else
            {
                SetStatusCard(LogStatus.Falha, "REPROVADO");
                Log("Foram encontradas falhas criticas. Revise os pontos acima com a TI local.", LogStatus.Falha);
            }
            startButton.IsEnabled = true;
            startButton.Content = "Iniciar Verificacao";
        }

        private async Task<bool> TestDnsAsync(IPAddress dnsServer, string fqdn, string srvName, int srvPort)
        {
            Log($"[26/27] Verificando DNS (Teste Independente contra {dnsServer})...", LogStatus.Titulo);
            var lookup = new LookupClient(dnsServer);
            bool dnsStatus = true;
            try
            {
                var resultA = await lookup.QueryAsync(fqdn, QueryType.A);
                var recordA = resultA.Answers.ARecords().FirstOrDefault();
                if (recordA != null)
                {
                    Log($"[Registro A] '{fqdn}' resolvido para: {recordA.Address}", LogStatus.OK);
                }
                else
                {
                    Log($"[Registro A] Nao foi possivel resolver '{fqdn}' (sem resposta A).", LogStatus.Falha);
                    dnsStatus = false;
                }
            }
            catch (Exception ex)
            {
                Log($"[Registro A] Erro ao consultar '{fqdn}': {ex.Message}", LogStatus.Falha);
                dnsStatus = false;
            }
            try
            {
                var resultSrv = await lookup.QueryAsync(srvName, QueryType.SRV);
                var recordSrv = resultSrv.Answers.SrvRecords().FirstOrDefault();
                if (recordSrv != null)
                {
                    if (recordSrv.Port == srvPort)
                    {
                        Log($"[Registro SRV] '{srvName}' resolvido para: {recordSrv.Target} (Porta: {recordSrv.Port})", LogStatus.OK);
                    }
                    else
                    {
                        Log($"[Registro SRV] Porta INCORRETA. Esperada: {srvPort}, Recebida: {recordSrv.Port}", LogStatus.Falha);
                        dnsStatus = false;
                    }
                }
                else
                {
                    Log($"[Registro SRV] Nao foi possivel resolver '{srvName}' (sem resposta SRV).", LogStatus.Falha);
                    dnsStatus = false;
                }
            }
            catch (Exception ex)
            {
                Log($"[Registro SRV] Erro ao consultar '{srvName}': {ex.Message}", LogStatus.Falha);
                dnsStatus = false;
            }
            return dnsStatus;
        }

        private async Task<bool> TestNtpAsync(IPAddress ntpServer)
        {
            Log($"[28] Verificando NTP (Teste Independente contra {ntpServer})...", LogStatus.Titulo);
            var ntpClient = new NtpClient(ntpServer);
            try
            {
                await ntpClient.SendAsync();
                Log($"Servidor NTP {ntpServer} respondeu com sucesso.", LogStatus.OK);
                return true;
            }
            catch (SocketException)
            {
                Log($"Servidor NTP {ntpServer} nao respondeu (Timeout). Causa: Firewall (Porta 123 UDP) ou servidor offline.", LogStatus.Falha);
                return false;
            }
            catch (Exception ex)
            {
                Log($"Erro ao consultar NTP {ntpServer}: {ex.Message}", LogStatus.Falha);
                return false;
            }
        }

        private void Log(string message, LogStatus status = LogStatus.Info)
        {
            Dispatcher.Invoke(() =>
            {
                var p = new Paragraph { Margin = new Thickness(0, 0, 0, 5) };
                var iconPath = new Path
                {
                    Height = 14,
                    Width = 14,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                switch (status)
                {
                    case LogStatus.OK:
                        iconPath.Data = (Geometry)FindResource("IconCheck");
                        iconPath.Fill = corIconeOK;
                        p.Inlines.Add(new InlineUIContainer(iconPath) { BaselineAlignment = BaselineAlignment.Center });
                        p.Inlines.Add(new Run($" {message}") { Foreground = corTexto });
                        break;
                    case LogStatus.Falha:
                        iconPath.Data = (Geometry)FindResource("IconFail");
                        iconPath.Fill = corIconeFalha;
                        p.Inlines.Add(new InlineUIContainer(iconPath) { BaselineAlignment = BaselineAlignment.Center });
                        p.Inlines.Add(new Run($" {message}") { Foreground = corTextoFalha, FontWeight = FontWeights.Bold });
                        break;
                    case LogStatus.Aviso:
                        iconPath.Data = (Geometry)FindResource("IconWarn");
                        iconPath.Fill = corIconeAviso;
                        p.Inlines.Add(new InlineUIContainer(iconPath) { BaselineAlignment = BaselineAlignment.Center });
                        p.Inlines.Add(new Run($" {message}") { Foreground = corIconeAviso });
                        break;
                    case LogStatus.Titulo:
                        p.Inlines.Add(new Run(message) { Foreground = corAcento, FontWeight = FontWeights.Bold, FontSize = 14 });
                        break;
                    case LogStatus.Info:
                    default:
                        p.Inlines.Add(new Run(message) { Foreground = corLog, FontStyle = FontStyles.Italic });
                        break;
                }
                logRichTextBox.Document.Blocks.Add(p);
                logRichTextBox.ScrollToEnd();
            });
        }

        private Inline GetIcon(string resourceKey, Brush color)
        {
            var path = new Path
            {
                Data = (Geometry)FindResource(resourceKey),
                Fill = color,
                Height = 14,
                Width = 14,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            };
            return new InlineUIContainer(path) { BaselineAlignment = BaselineAlignment.Center };
        }

        private void SetStatusCard(LogStatus status, string text)
        {
            StatusCard.Visibility = Visibility.Visible;
            StatusText.Text = text;
            switch (status)
            {
                case LogStatus.OK:
                    StatusCard.Background = statusOK;
                    StatusText.Foreground = corTextoOK;
                    break;
                case LogStatus.Falha:
                    StatusCard.Background = statusFalha;
                    StatusText.Foreground = corTextoFalha;
                    break;
                case LogStatus.Info:
                default:
                    StatusCard.Background = statusAguarde;
                    StatusText.Foreground = corTextoAguarde;
                    break;
            }
        }

        public class AdapterItem
        {
            public string DisplayName { get; set; } = "";
            public NetworkInterface Interface { get; set; } = null!;
        }

        public class DhcpOptions
        {
            public IPAddress[] DnsServers { get; set; } = Array.Empty<IPAddress>();
            public IPAddress[] NtpServers { get; set; } = Array.Empty<IPAddress>();
            public string DomainName { get; set; } = "";
        }

        public class NtpClient
        {
            private readonly IPEndPoint _endPoint;
            public NtpClient(IPAddress server) { _endPoint = new IPEndPoint(server, 123); }
            public async Task SendAsync(int timeout = 3000)
            {
                using var udpClient = new UdpClient();
                udpClient.Client.ReceiveTimeout = timeout;
                var sntpPacket = new byte[48];
                sntpPacket[0] = 0x1B;
                await udpClient.SendAsync(sntpPacket, sntpPacket.Length, _endPoint);
                var result = await udpClient.ReceiveAsync();
                if (result.Buffer.Length < 48) throw new Exception("Resposta NTP invalida.");
            }
        }

        public class DhcpClient
        {
            private readonly UdpClient _client;
            private readonly IPAddress _localIp;
            public DhcpClient(IPAddress localIp)
            {
                _localIp = localIp;
                _client = new UdpClient(new IPEndPoint(_localIp, 68));
                _client.Client.ReceiveTimeout = 5000;
            }
            public async Task<DhcpOptions?> RequestDhcpOptionsAsync(PhysicalAddress mac)
            {
                var packet = BuildDhcpInformPacket(mac);
                await _client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 67));
                try
                {
                    var result = await _client.ReceiveAsync();
                    return ParseDhcpAckPacket(result.Buffer);
                }
                catch (SocketException) { return null; }
                finally { _client.Close(); }
            }
            private byte[] BuildDhcpInformPacket(PhysicalAddress mac)
            {
                var packet = new byte[300];
                int i = 0;
                packet[i++] = 0x01; packet[i++] = 0x01; packet[i++] = 0x06; packet[i++] = 0x00;
                BitConverter.GetBytes(0x12345678).CopyTo(packet, i); i += 4;
                i += 4; // secs, flags
                _localIp.GetAddressBytes().CopyTo(packet, i); i += 4;
                i += 12; // yiaddr, siaddr, giaddr
                mac.GetAddressBytes().CopyTo(packet, i); i += 6;
                i += (10 + 64 + 128); // sname, file
                packet[i++] = 0x63; packet[i++] = 0x82; packet[i++] = 0x53; packet[i++] = 0x63;
                packet[i++] = 53; packet[i++] = 1; packet[i++] = 8;
                var vendor = Encoding.ASCII.GetBytes("MSFT 5.0");
                packet[i++] = 60; packet[i++] = (byte)vendor.Length;
                vendor.CopyTo(packet, i); i += vendor.Length;
                byte[] requestedOptions = { 1, 3, 6, 15, 42 };
                packet[i++] = 55; packet[i++] = (byte)requestedOptions.Length;
                requestedOptions.CopyTo(packet, i); i += requestedOptions.Length;
                packet[i++] = 255;
                return packet;
            }
            private DhcpOptions ParseDhcpAckPacket(byte[] buffer)
            {
                var options = new DhcpOptions();
                int i = 240;
                while (buffer[i] != 255 && i < buffer.Length - 2)
                {
                    byte option = buffer[i++];
                    byte len = buffer[i++];
                    byte[] data = new byte[len];
                    Buffer.BlockCopy(buffer, i, data, 0, len);
                    i += len;
                    switch (option)
                    {
                        case 6:
                            options.DnsServers = Enumerable.Range(0, len / 4).Select(n => new IPAddress(data.Skip(n * 4).Take(4).ToArray())).ToArray();
                            break;
                        case 15:
                            options.DomainName = Encoding.ASCII.GetString(data).TrimEnd('\0');
                            break;
                        case 42:
                            options.NtpServers = Enumerable.Range(0, len / 4).Select(n => new IPAddress(data.Skip(n * 4).Take(4).ToArray())).ToArray();
                            break;
                    }
                }
                return options;
            }
        }
    }
}