# Project Dashboard 🚀

O **Project Dashboard** é uma ferramenta pra te ajudar a organizar, rastrear e gerenciar todos os projetos de código (repositórios Git) que ficam espalhados pelo seu computador.

Sabe quando você tem dezenas de pastas com testes, projetos antigos, repositórios de clientes, e acaba perdendo a noção do que tem ali? Esse dashboard resolve isso fazendo um scan nas suas pastas e montando um painel bonito e interativo com tudo o que ele encontra.

Ele possui uma versão Desktop construída em **.NET 9** (via .NET MAUI / Avalonia) e uma nova versão Web independente construída em **Ruby on Rails** e **Tailwind CSS**. Ambas rodam 100% localmente com bancos SQLite.

---

## ✨ O que ele faz?

- 🔍 **Auto-Scan de Repositórios**: Você diz em quais pastas seus projetos costumam ficar (ex: `D:\projects`), ele varre os diretórios procurando tudo que tem `.git` e indexa pra você automaticamente.
- 📊 **Metadados Ricos**: O scan vai além do básico. Ele conta os commits, lista os contribuidores, puxa as últimas mensagens de commit, conta branches e até busca por arquivos `.md` (sua documentação!).
- 🛠️ **Reconhecimento de Tech Stack**: Ele lê arquivos como `package.json`, `Dockerfile`, `.csproj`, etc., e deduz quais tecnologias você usou no projeto (ex: React, Node, Docker, C#, Python), criando tags automáticas.
- 📝 **Anotações e Metas**: Cada projeto tem uma página de detalhes onde você pode criar tags manuais, escrever anotações soltas (com auto-save) e criar checklists de metas pro futuro.
- 🗂️ **Organização Visual**: Filtre seus projetos pelas tecnologias usadas, veja os projetos mais recentes que você tocou, e "pine" (fixe) os que você usa com mais frequência.
- 💻 **Web & Desktop**: Dá pra rodar via navegador ou como um app Windows nativo. Na versão instalada no pc, você tem até acesso ao selecionador de pastas nativo do Explorer.

---

## 🛠️ Tecnologias Utilizadas

A arquitetura do projeto é dividida em partes independentes: `Shared/Desktop` (C#/.NET) e `WebRails` (Ruby on Rails).

- **.NET 9** e **C#** (App Desktop)
- **Ruby on Rails 8** (App Web)
- **Tailwind CSS v4** (Estilização baseada em classes utilitárias)
- **LibGit2Sharp** (Desktop) e **Git CLI nativo** (Web) para ler os repositórios reais sem depender da CLI externa onde possível.
- **Entity Framework Core + SQLite** (Banco de dados local embutido em `%localappdata%\ProjectDashboard` no Desktop, e db local do projeto no Rails)
- **.NET MAUI / Avalonia** (Para empacotar a versão desktop)

---

## 🚀 Como rodar na sua máquina

Certifique-se de que você tem o **.NET 9 SDK** instalado antes de começar.

### Para rodar a versão Desktop (App do Windows):

Se você quer a experiência completa com a janela nativa do Windows, rodar o MAUI é a melhor opção:

```bash
# Navegue até a pasta raiz do repositório
cd caminho/pro/projeto

# Rode com o comando do MAUI
dotnet run --project src/Desktop -f net9.0-windows10.0.19041.0
```

### Para rodar a versão Web (Browser com Ruby on Rails):

Certifique-se de que você tem o **Ruby 4.0** (ou 3.x) instalado.

```bash
# Navegue até a pasta do painel web
cd Src/WebRails

# Instale as dependências (primeira vez)
bundle install
ruby bin/rails db:prepare

# Rode o servidor
ruby bin/rails server
```

Ele deve ficar disponível no link `http://localhost:3000`.

---

## 💡 Como usar

1. Logo que abrir, a lista vai estar vazia. Clique no botão azul **Scan Projects** lá no topo.
2. Na versão Web, digite o caminho da pasta raiz onde ficam seus repos (ex: `C:\Users\gabs\Documentos\GitHub`) e clique em Add.
3. Se tiver no app Desktop, é só clicar em **Browse** e escolher a pasta pelo explorador do Windows normal.
4. Clique em **Start Scan**. Ele vai varrer todas as pastas procurando projetos, ler tudo ignorando pastas pesadas (como `node_modules` e `bin`) pra ser bem rápido.
5. Pronto! É só explorar clicando nos cards pra ver os detalhes de cada um. O banco de dados fica salvo na sua pasta local do sistema, então você não perde os dados se fechar.

---

## 🤝 Quer contribuir?

Sinta-se livre pra abrir issues se o scanner travar numa pasta bizarra, ou mandar um pull request melhorando a busca de linguagens. Toda ajuda é bem-vinda!
