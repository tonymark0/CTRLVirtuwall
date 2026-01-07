<div align="center">

# 🌐 CTRLVirtuwall  
### Ferramenta de Diagnóstico de Rede para Implantação Barco CTRL

O **CTRLVirtuwall** é um utilitário de auditoria desenvolvido em **C# (WPF)** para validar se a infraestrutura de rede de um cliente atende aos pré-requisitos críticos antes da instalação do sistema **Barco CTRL**.

</div>

---

## ✨ Diferenciais

- **Raw Network Queries**: Ignora o cache do Windows e serviços locais para obter respostas reais do servidor **DHCP/DNS**.  
- **Diagnóstico de Protocolos**: Validação profunda das opções DHCP **6 (DNS)**, **15 (Domínio)** e **42 (NTP)**.  
- **Veredito Instantâneo**: Interface visual que indica claramente se o ambiente está **"Aprovado"** ou **"Reprovado"**.  
- **Zero Instalação**: Disponível em versão **Self-Contained (Portátil)**.

---

## 🚀 Como Utilizar

### Pré-requisitos

> [!IMPORTANT]  
> **Privilégios de Administrador:**  
> A aplicação deve ser executada como **Administrador** para permitir o envio de pacotes de broadcast **DHCPINFORM** e a escuta na porta **UDP 68**.

### Passo a passo

1. Acesse a aba **Releases** e baixe a versão mais recente.  
2. Clique com o botão direito no executável e selecione **Executar como Administrador**.  
3. Selecione a **Interface de Rede ativa** (Ethernet ou Wi-Fi).  
4. Configure os parâmetros do projeto:  
   - **FQDN**: Nome completo do servidor  
   - **Domínio**: Sufixo de rede esperado  
   - **Porta**: Porta do serviço SRV *(padrão: 8883)*  
5. Clique em **Iniciar Verificação**.

---

## 🛠️ Detalhes Técnicos

A ferramenta realiza **três níveis de testes independentes**:

| Teste           | Objetivo                                              | Protocolo    |
|----------------|--------------------------------------------------------|-------------|
| **DHCP Core**  | Valida as opções **6**, **15** e **42** enviadas pelo servidor | UDP 67/68   |
| **DNS Resolve**| Testa resolução de **Registro A** e **Registro SRV**  | UDP 53      |
| **NTP Sync**   | Verifica se o servidor de tempo está respondendo       | UDP 123     |

---

## ⚙️ Opções de Compilação

- **Self-Contained (Recomendado):**  
  Inclui o runtime do **.NET 8** dentro do `.exe`.  
  Maior tamanho (~70MB), mas funciona em qualquer máquina.

- **Framework-Dependent:**  
  Arquivo leve (<1MB), requer que o cliente tenha o **.NET 8 Desktop Runtime** instalado.

---

## 👨‍💻 Créditos & Suporte

Desenvolvido por **Marco Antonio**.  
© 2025 Virtuwall. Todos os direitos reservados.

<div align="center">
  <sub>Construído com ❤️ e .NET 8.0</sub>
</div>
