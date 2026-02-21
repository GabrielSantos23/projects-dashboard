# Project Dashboard 🚀

O **Project Dashboard** é uma ferramenta pra te ajudar a organizar, rastrear e gerenciar todos os projetos de código (repositórios Git) que ficam espalhados pelo seu computador.

Sabe quando você tem dezenas de pastas com testes, projetos antigos, repositórios de clientes, e acaba perdendo a noção do que tem ali? Esse dashboard resolve isso fazendo um scan nas suas pastas e montando um painel bonito e interativo com tudo o que ele encontra.

Ele foi construído em **.NET 9** usando **Blazor** e **Tailwind CSS**, e roda 100% localmente com um banco SQLite. Além disso, a interface principal é compartilhada pra rodar tanto como uma aplicação Web quanto como um app Desktop nativo do Windows (via .NET MAUI).

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

A arquitetura do projeto é dividida em 3 partes: `Shared` (onde a mágica acontece), `Web` (Blazor Server) e `Desktop` (MAUI Hybrid).

- **.NET 9** e **C#**
- **Blazor** (Frontend reativo)
- **Tailwind CSS** (Estilização inteira baseada em classes utilitárias)
- **LibGit2Sharp** (Pra ler os repositórios reais sem precisar rodar comandos git pelo terminal)
- **Entity Framework Core + SQLite** (Banco de dados local embutido em `%localappdata%\ProjectDashboard`)
- **.NET MAUI** (Pra empacotar a versão desktop)

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

### Para rodar a versão Web (Browser):

Se você prefere abrir pelo Chrome/Edge/Firefox:

```bash
# Na raiz, use o dotnet watch para dev com Hot Reload
dotnet watch run --project src/Web
```

Ele deve abrir sozinho no link `http://localhost:5276` (ou porta parecida que o terminal te devolver).

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
