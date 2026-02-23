# Project Dashboard

![Capa do Project Dashboard](capa.png)

O **Project Dashboard** é uma aplicação criada para organizar e acompanhar todos os seus projetos Git que ficam espalhados pelo computador.

Se organize com todas as pastas de projetos, versões antigas, freelas, projetos de estudo, centralize tudo em um único lugar. O aplicativo scaneia as pastas que você escolher e mostra todos os repositórios encontrados.

---

[![Download for Windows](https://img.shields.io/badge/Download_for-Windows-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/GabrielSantos23/projects-dashboard/releases/latest)

## O que ele faz?

- **Scan automático de repositórios**
  Você informa as pastas onde costuma guardar seus projetos, o app procura os diretórios e busca pastas com `.git` e lista tudo automaticamente.

- **Leitura de metadados do Git**
  Para cada projeto encontrado, o app pega informações como número de commits, contribuidores, últimos commits, branches e atividade recente.

- **Reconhecimento de tecnologias**
  O sistema analisa arquivos como `package.json`, `Dockerfile`, `.csproj`, etc para identificar quais tecnologias estão sendo usadas e cria tags automaticamente.

- **Organização e anotações**
  Cada projeto tem uma página de detalhes onde você pode adicionar tags manuais, fixar projetos importantes e realizar ações como stage, commit e push.

- **Versão Web e Desktop**
  Você pode usar pelo navegador ou como aplicativo nativo no Windows.

- **Atualizações automáticas**
  A versão Desktop verifica versões no GitHub e atualiza automaticamente.

---

## Tecnologias Usadas

| Tecnologia                | Descrição                                                                          | Onde é usado      |
| :------------------------ | :--------------------------------------------------------------------------------- | :---------------- |
| **.NET 9 / C#**           | Linguagem e Framework do sistema                                                   | App Desktop       |
| **Avalonia UI**           | Framework de interface (XAML)                                                      | App Desktop       |
| **Entity Framework Core** | ORM para o acesso a dados locais                                                   | App Desktop       |
| **LibGit2Sharp**          | Integração C# com a biblioteca para leitura de repositórios                        | App Desktop       |
| **SQLite**                | Banco de dados local                                                               | App Web & Desktop |
| **Ruby on Rails**         | Framework Web estruturado em MVC.                                                  | App Web           |
| **Tailwind CSS**          | Framework de estilização na web.                                                   | App Web           |
| **GitHub Actions**        | Scripts de pipeline que automatizam o processo inteiro de build e update contínuo. | Infra             |

---

## Como instalar e rodar

Você pode usar o Project Dashboard de três formas: baixando o app pronto, compilando o app Desktop ou rodando a versão web.

### 1. Baixar o app pronto

Se você quer apenas usar o aplicativo:

1. entre na página de [Releases](https://github.com/GabrielSantos23/projects-dashboard/releases).
2. Baixe o arquivo `ProjectDashboard_Setup.exe`.
3. Execute o arquivo `.exe`.

O aplicativo é autossuficiente. Não é necessário instalar o .NET para usar ele. Ele também atualiza automaticamente quando novas versões são lançadas.

---

### 2. Compilar o App Desktop pelo código-fonte

Se você prefere compilar o projeto manualmente, é necessário ter o **.NET 9 SDK** instalado.

```bash
git clone https://github.com/GabrielSantos23/projects-dashboard.git
cd projects-dashboard

dotnet run --project Src/DesktopAvalonia/ProjectDashboard.Avalonia.csproj
```

---

### 3. Rodar a versão Web (Ruby on Rails)

A versão Web foi feita com Ruby on Rails 9. É preciso ter o **Ruby**.

```bash
cd Src/WebRails

bundle install
ruby bin/rails db:prepare

ruby bin/rails server
```

Depois disso, o painel estará disponível em:

```
http://localhost:3000
```

---

## Como usar

1. Ao abrir o aplicativo pela primeira vez, a lista de projetos estará vazia, clique em **Scan Projects**.
2. Selecione a pasta raiz onde ficam seus repositórios.
3. Clique em **Add Folder** e depois em **Start Scan**.
4. O app vai procurar por projetos Git, ignorando pastas pesadas como `node_modules` ou `bin` para manter o desempenho.
5. Após o scan, os projetos aparecerão no app.
6. Clique em qualquer projeto para abrir a página de detalhes, você pode adicionar tags, visualizar informações do Git e realizar commit.
7. O banco de dados é salvo localmente.
8. Use o botão **Refresh** para atualizar as informações e buscar novos commits ou projetos adicionados recentemente.

---

## Contribuições

Se encontrar algum problema (por exemplo, o scanner travando em alguma pasta), fique à vontade para abrir uma issue.
Pull requests com melhorias na detecção de tecnologias ou na interface também são bem-vindos.
