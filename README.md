# Source Conquer Online 5095 / 5517 - Servidor Privado em C# (Open Source)

**source completa para servidor privado de Conquer Online**, originalmente baseada na versão 5517 e compatível com o cliente clássico **versão 5095**. Desenvolvida inteiramente em **C#**, essa base tem sido utilizada por mim como ferramenta de estudo e aprimoramento técnico no desenvolvimento de servidores.

## Sobre o Projeto

- Baseado na versão 5517, mas com compatibilidade total com o cliente 5095
- Código escrito em C#
- Contém AccServer (autenticação) e GameServer (lógica do jogo)
- Repleto de correções, otimizações e melhorias de estabilidade
- Sistema de eventos automáticos, shop, PvP, guilds, etc.
- Banco de dados MySQL incluso no formato `.zq`

## Estrutura do Projeto

```
AccServer/     - Servidor de autenticação (login, conexão com DB)
GameServer/    - Toda lógica do jogo, eventos, controle de players
Database/      - Arquivo .zq com estrutura MySQL para uso direto
```

## Requisitos

- Visual Studio
- Cliente Conquer Online versão 5095
- MySQL ou MariaDB
- Navicat (ou similar) para importar o banco de dados `.zq`

## Como Rodar

1. Clone este repositório
2. No `AccServer`, edite a string de conexão:
   - Exemplo: `Database=zq;Uid=root;Password=123456789`
3. No `GameServer`, altere a senha `Higor123*` para a mesma senha do banco
4. Importe o banco de dados `.zq` no MySQL (recomendo Navicat)
5. Compile os projetos no Visual Studio
6. Execute `AccServer.exe` e `GameServer.exe`

## Recursos Implementados

- **Offline Market** – Sistema de comércio funcionando mesmo com o personagem desconectado.
- **Offline Miner** – Mineração automática enquanto o jogador está offline.
- **Integração com Discord (Discord API)** – Atualizações e interações conectadas ao servidor do Discord.
- **Online Points** – Sistema de pontos por tempo online.
- **Sistema VIP** – Benefícios exclusivos para jogadores VIP.
- **Socket System** – Sistema completo de sockets para itens.
Entre outras infinidades de coisas!

## Principais Palavras-Chave (SEO)

- Source Conquer Online 5095
- Servidor privado Conquer Classic
- Conquer 5095 Open Source
- Criar servidor Conquer Online
- C# Conquer Server
- GitHub Conquer Online

## Licença e Uso

Este projeto é open-source e pode ser utilizado para fins de **estudo**, **desenvolvimento próprio**, **educação**.

Não é permitida a revenda do projeto.

## Contato e Portfólio

Caso tenha interesse em conversar sobre o projeto, colaborar ou contratar serviços de desenvolvimento:

- GitHub: https://github.com/cnthigu
- LinkedIn: https://linkedin.com/in/higor-cnt-vinicius
- E-mail: higorzen77@gmail.com

## Contribua ou Deixe uma Estrela

Se este projeto foi útil para você, considere deixar uma estrela ⭐ aqui no GitHub. Isso ajuda a manter o projeto ativo e visível para mais pessoas.

---
