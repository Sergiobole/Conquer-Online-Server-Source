# Conquer Online - Servidor Source

## Sobre o Projeto

Este projeto é a criação e desenvolvimento de um servidor source de **Conquer Online**, utilizando a versão **5517**, mas com diversas modificações e melhorias. O servidor foi projetado para oferecer uma experiência clássica de Conquer, com ajustes modernos e correções de bugs, proporcionando um gameplay mais estável e divertido.

O projeto está estruturado em dois componentes principais:
1. **Account Server (AccServer)**: Responsável pela autenticação e gerenciamento de contas.
2. **Game Server (GameServer)**: Responsável pela lógica do jogo, interações e eventos dentro do servidor.

---

## Configuração

### 1. Alterações no `AccServer`
Dentro do arquivo de configuração do **AccServer**, é necessário ajustar a conexão com o banco de dados. 

- Abra o arquivo de configuração no **AccServer** e altere a string de conexão para:
  Database=zq;Uid=root;Password=123456789
Onde:
- **Database**: é o nome do banco de dados.
- **Uid**: usuário para acesso ao banco de dados.
- **Password**: senha do banco de dados.

### 2. Alterações no `GameServer`
No **GameServer**, o arquivo de configuração também requer ajustes na senha e no nome da base de dados. 

- Procure por `Higor123*` dentro do código e altere para a senha que você configurou no **AccServer** e **AppServer**.
- Também altere o nome da base de dados para coincidir com a configuração que você usou no **AccServer**.

Após realizar essas alterações, **build** o projeto e inicie os servidores normalmente.

### 3. Banco de Dados
O banco de dados necessário para o servidor é o arquivo **`.zq`**. Após clonar o repositório, siga os seguintes passos para configurar o banco de dados:

1. Abra o **Navicat** (ou qualquer outro cliente MySQL).
2. Importe o arquivo **`.zq`** para o banco de dados.
3. Certifique-se de que a configuração de usuário e senha esteja correta para que a conexão com o banco de dados funcione.

---

## Funcionalidades

✅ **Servidor Estável**: Baseado na versão 5095, com todas as funcionalidades do Conquer Online clássico.

✅ **Correções e Melhorias**: Diversos bugs corrigidos, incluindo melhorias no desempenho e estabilidade.

✅ **Eventos Programados**: Sistema de eventos configurado para proporcionar uma experiência de jogo mais dinâmica e competitiva.

✅ **Sistema de Loja Online**: Integração com **Assas** e outras formas de pagamento para facilitar a compra de créditos no jogo.

---

## Estrutura do Projeto

📂 **AccServer/** → Gerenciamento de contas, autenticação e banco de dados.

📂 **GameServer/** → Lógica do jogo, controle de eventos e interação entre jogadores.

📂 **Database/** → Estrutura de banco de dados, incluindo as tabelas e a configuração de usuários.

---

## Como Rodar o Projeto

1. **Configuração do Banco de Dados**:
 - Certifique-se de que você tenha o banco de dados configurado corretamente (MySQL ou MariaDB).
 - Após clonar o repositório, use o **Navicat** ou outro cliente MySQL para importar o banco de dados **`.zq`**.

2. **Build do Projeto**:
 - Compile e execute os projetos **AccServer** e **GameServer**.
 - Acesse o **GameServer** e **AccServer** para confirmar se estão funcionando corretamente.

---

## Habilidades Demonstradas

🚀 Desenvolvimento de **servidor privado** de **Conquer Online** com **C#**.

🔧 **Correções de bugs** e **melhorias** na experiência de jogo.

🛠️ **Integração de sistemas** e gerenciamento de servidores de jogos.

---

## Contato

Se precisar de mais informações ou quiser discutir o projeto, entre em contato comigo:

📧 **E-mail:** [higorzen77](mailto:higorzen77@gmail.com)

🐙 **GitHub:** [github.com/cnthigu](https://github.com/cnthigu)

🔗 **LinkedIn:** [linkedin.com/in/higor-cnt-viniciusl](www.linkedin.com/in/higor-cnt-vinicius)

---

🔹 **Gostou do projeto?** Considere deixar uma ⭐ para apoiar o desenvolvimento!
