# Verificador CTRL Virtuwall

![Ícone da Aplicação](https://i.imgur.com/gA3qjHw.png) Ferramenta de diagnóstico profissional desenvolvida em C# (WPF) para auditar a infraestrutura de rede de um cliente antes da implantação do sistema Barco CTRL.

Esta aplicação gera um "atestado" rápido, validando se todos os pré-requisitos críticos de rede (DHCP, DNS e NTP) estão configurados corretamente, ajudando a prevenir falhas de implantação no local do cliente.

---

## Funcionalidades Principais

Esta ferramenta é 100% independente dos serviços locais do Windows para garantir um teste de rede verdadeiro:

* **Interface Gráfica Nativa:** Uma interface limpa, profissional e responsiva (WPF .NET 8) sem dependências externas de UI.
* **Verificação de DHCP (100% Independente):** Envia um pacote `DHCPINFORM` (Broadcast) para consultar o servidor DHCP diretamente pelas Opções de rede, em vez de confiar no que o Windows *acha* que recebeu.
    * Valida a **Opção 6 (Servidor DNS)**.
    * Valida a **Opção 15 (Sufixo de Domínio)**.
    * Valida a **Opção 42 (Servidor NTP)**.
* **Verificação de DNS (Independente):** Consulta o servidor DNS (fornecido pela Opção 6) diretamente, ignorando o cache de DNS do Windows, para:
    * Resolver o **Registo A** (Host).
    * Resolver o **Registo SRV** (_barcomanagement._tcp...).
* **Verificação de NTP (Independente):** Envia um pacote SNTP para o servidor NTP (fornecido pela Opção 42) para confirmar que ele está online e a responder na porta 123 (UDP), ignorando o serviço de Tempo do Windows.
* **Relatório Detalhado:** Fornece um log claro e em tempo real com ícones de status (?, ?, ??) e um "Cartão de Status" final (APROVADO / REPROVADO).

---

## Como Usar

**?? REQUISITO CRÍTICO: EXECUTAR COMO ADMINISTRADOR**

Para que o teste de DHCP (DHCPINFORM) possa enviar pacotes de rede de baixo nível, a aplicação **DEVE** ser executada com privilégios de Administrador.

1.  Clique com o botão direito no `.exe`.
2.  Selecione **"Executar como administrador"**.
3.  O aviso do UAC (Fornecedor Desconhecido) aparecerá. Clique em "Sim".
4.  Selecione o **Adaptador de Rede** correto (ex: "Ethernet" ou "Wi-Fi").
5.  Preencha os campos `FQDN`, `Domínio Base` e `Porta` com os dados do projeto.
6.  Clique em **"Iniciar Verificacao"**.
7.  Analise o relatório gerado.

---

## Builds (Versões do .exe)

Este repositório pode gerar duas versões do executável.

### 1. Versão Autocontida (Self-Contained) - RECOMENDADA

Esta é a versão "toda embarcada" (mais pesada).

* **Tamanho:** Aprox. 60-80 MB.
* **Prós:** 100% portátil. Contém o .NET 8 e todas as dependências (como o `DnsClient.dll`) "embutidas" dentro do `.exe`.
* **Contras:** Ficheiro de maior tamanho.
* **Use esta versão:** Para levar para o cliente, garantindo que a ferramenta funciona em qualquer máquina Windows (x64), mesmo que ela não tenha o .NET 8 instalado.

### 2. Versão Dependente de Framework (Framework-Dependent)

Esta é a versão "mais leve".

* **Tamanho:** Aprox. < 1 MB.
* **Prós:** Ficheiro muito pequeno e rápido de compilar.
* **Contras:** Exige que o **".NET 8 Desktop Runtime"** já esteja instalado na máquina do cliente. Se não estiver, a aplicação não abre.
* **Use esta versão:** Apenas para desenvolvimento ou se você tiver 100% de certeza que a máquina de destino já possui o .NET 8.

---

## Créditos

Desenvolvido por **Marco Antonio**.
(c) 2025 **Virtuwall**. Todos os direitos reservados.