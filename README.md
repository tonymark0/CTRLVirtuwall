🌐 CTRLVirtuwall

O CTRLVirtuwall é uma ferramenta de diagnóstico de alto desempenho desenvolvida para auditar infraestruturas de rede antes da implantação do sistema Barco CTRL. Ele garante que os serviços vitais (DHCP, DNS e NTP) estejam operacionais e corretamente configurados.

✨ Destaques

Zero Dependências do SO: Realiza consultas brutas (raw queries) de rede, ignorando o cache do Windows para resultados 100% reais.

Diagnóstico Profundo: Valida Opções DHCP 6, 15 e 42 de forma independente.

Relatórios Instantâneos: Gera um veredito "Aprovado/Reprovado" com logs detalhados para a equipe de TI.

UI Moderna: Interface baseada em WPF com design responsivo e indicadores visuais claros.

🚀 Como Utilizar

Pré-requisitos

[!IMPORTANT]
Privilégios de Administrador: São obrigatórios para realizar o broadcast DHCPINFORM e capturar pacotes de rede de baixo nível.

Baixe o executável na aba Releases.

Execute como Administrador.

Selecione a interface de rede (Ethernet/Wi-Fi) correta.

Insira os parâmetros do projeto (FQDN, Domínio e Porta).

Clique em Iniciar Verificação.

🛠️ Detalhes Técnicos

Verificações Realizadas

DHCP Core: Envio de pacote DHCPINFORM para validar as configurações distribuídas pelo servidor local.

DNS Resolve: Consulta direta ao servidor DNS identificado, buscando Registros A e SRV (_barcomanagement._tcp).

NTP Sync: Validação de sincronia de tempo via protocolo SNTP na porta 123 UDP.

Opções de Build

Self-Contained (Recomendado): Um único .exe de ~70MB que funciona em qualquer Windows sem necessidade de instalar o .NET.

Framework-Dependent: Arquivo leve (<1MB) que exige o .NET 8 Runtime instalado no cliente.

👨‍💻 Créditos & Suporte

Desenvolvido por Marco Antonio.
© 2025 Virtuwall. Todos os direitos reservados.
