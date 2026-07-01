# fcg-catalog-api

Microsserviço de catálogo do projeto FCGames — Tech Challenge FIAP Fase 2.

Responsável pelo CRUD de jogos, pela biblioteca do usuário e pela orquestração da
compra (inicia o fluxo assíncrono de pagamento via RabbitMQ). É o serviço que **conecta**
os quatro microsserviços na jornada de compra.

> Pasta local: `fcg-catalog-service` · Repositório GitHub: `fcg-catalog-api`

---

## Arquitetura

```
Fiap.FCGames.Catalogo.Api        — HTTP :5002  (jogos, compras, biblioteca + health)
Fiap.FCGames.Catalogo.Worker     — Worker Service (consumers RabbitMQ)
Fiap.FCGames.Catalogo.Domain     — Jogo, Biblioteca, ItemBiblioteca, Pedido + enum StatusPedido
Fiap.FCGames.Catalogo.Infra      — EF Core, repositories, migrations
Fiap.FCGames.Catalogo.Application — Commands/Queries (CQRS/MediatR)
Fiap.FCGames.Catalogo.CrossCutting — JWT, Swagger, Serilog, MassTransit extensions
```

### Fluxo de mensagens

```
UsersAPI
  └── publica UsuarioCriadoEvento
        └── [fila: catalog-usuario-criado]
              └── Catalog.Worker cria a Biblioteca vazia (idempotente por UsuarioId)

Cliente → POST /compras
  └── Catalog.Api cria Pedido(Pendente) + grava evento no Outbox (mesma transação)
        └── publica PedidoRealizadoEvento
              └── PaymentsAPI processa e publica PagamentoProcessadoEvento
                    └── [fila: catalog-pagamento-processado]
                          └── Catalog.Worker consome (Inbox/dedup)
                                ├── Aprovado → Pedido=Aprovado + ItemBiblioteca
                                └── Rejeitado → Pedido=Rejeitado
```

---

## Endpoints

### `GET /jogos` — [Authorize]
Lista o catálogo de jogos.

### `POST /jogos` — [Authorize Admin]
Cadastra um jogo. **Response 201** com o `id` gerado.
```json
{ "nome": "Hades", "descricao": "Roguelike", "preco": 49.90 }
```

### `PUT /jogos/{id}` — [Authorize Admin]
Atualiza um jogo. `404` se não existir.

### `DELETE /jogos/{id}` — [Authorize Admin]
Remove um jogo. `404` se não existir.

### `POST /compras` — [Authorize]
Inicia a compra (fluxo assíncrono). O `UsuarioId` vem do JWT, não do body.
```json
{ "jogoId": "guid-do-jogo" }
```
Comportamento: valida jogo (`404`) e duplicidade (`409`) → cria `Pedido(Pendente)` →
publica `PedidoRealizadoEvento` (via Outbox) → **Response 202**.
```json
{ "orderId": "guid", "jogoId": "guid", "nomeJogo": "Hades", "preco": 49.90, "status": "Pendente" }
```

### `GET /compras/{orderId}` — [Authorize]
Consulta o status do pedido: `Pendente | Aprovado | Rejeitado`.

### `GET /biblioteca/{usuarioId}` — [Authorize]
Lista os jogos que o usuário possui.
```json
{ "usuarioId": "guid", "jogos": [ { "jogoId": "guid", "nomeJogo": "Hades", "preco": 49.90, "dataAdicao": "2026-06-30T10:00:00Z" } ] }
```

### `GET /health` — sem auth
Liveness/readiness probe. **Response 200:** `{ "status": "Healthy" }`

---

## Banco de Dados

**catalog-db** (SQLite):

```sql
Jogos(Id PK, Nome, Descricao, Preco, DataCadastro)
Bibliotecas(Id PK, UsuarioId UNIQUE, CriadaEm)
ItensBiblioteca(Id PK, BibliotecaId FK, JogoId, DataAdicao, UNIQUE(BibliotecaId, JogoId))
Pedidos(Id PK, UsuarioId, JogoId, Preco, Status INTEGER, CriadoEm)  -- Status: 0=Pendente,1=Aprovado,2=Rejeitado
```

Tabelas de infraestrutura do **Transactional Outbox** (MassTransit): `InboxState`,
`OutboxMessage`, `OutboxState`. A migration é aplicada automaticamente no startup da API
e do Worker (`Database.Migrate()`).

---

## Eventos

### Consome

**`UsuarioCriadoEvento`** — publicado pelo UsersAPI · fila `catalog-usuario-criado`
→ cria a `Biblioteca` vazia (idempotente por `UsuarioId`).

**`PagamentoProcessadoEvento`** — publicado pelo PaymentsAPI · fila `catalog-pagamento-processado`
→ Aprovado: `Pedido=Aprovado` + `ItemBiblioteca`; Rejeitado: `Pedido=Rejeitado`.

Ambos os endpoints usam **Inbox** (`UseEntityFrameworkOutbox`) para deduplicação transacional.

### Publica

**`PedidoRealizadoEvento`** — consumido pelo PaymentsAPI
```csharp
record PedidoRealizadoEvento(
    Guid PedidoId, Guid UsuarioId, Guid JogoId, string NomeJogo,
    decimal Preco, DateTime RealizadoEmUtc, Guid CorrelationId);
```
Publicado via **Bus Outbox**: o evento é gravado na mesma transação do `Pedido` e
entregue ao broker em background (não se perde se o RabbitMQ estiver fora).

---

## Variáveis de Ambiente

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `JWT__KEY` | Chave de assinatura JWT (mínimo 32 chars) | `MinhaChaveSegredo...` |
| `JWT__ISSUER` | Issuer validado no token | `AppFiapFcGames` |
| `ConnectionStrings__DefaultConnection` | Connection string do banco | `Data Source=/data/catalog.db` |
| `RabbitMQ__Host` | Host do RabbitMQ | `rabbitmq` |
| `RabbitMQ__Username` | Usuário RabbitMQ | `guest` |
| `RabbitMQ__Password` | Senha RabbitMQ | `guest` |

> **Segredos nunca devem estar no `appsettings.json` de produção.** Use env vars ou Kubernetes secrets.

---

## Rodando Localmente

### Pré-requisitos
- .NET 10 SDK
- RabbitMQ em `localhost:5672` (ou via Docker)
- `NUGET_AUTH_TOKEN` com scope `read:packages` (restaura `FCGames.IntegrationEvents`)

### 1. Configurar NUGET_AUTH_TOKEN
```bash
export NUGET_AUTH_TOKEN=ghp_...        # PowerShell: $env:NUGET_AUTH_TOKEN="ghp_..."
```

### 2. Restaurar e rodar a API
```bash
cd app/src
dotnet run --project Fiap.FCGames.Catalogo.Api
# http://localhost:5002/swagger
```

### 3. Rodar o Worker (terminal separado)
```bash
cd app/src
dotnet run --project Fiap.FCGames.Catalogo.Worker
```

### RabbitMQ via Docker (rápido)
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
# Management UI: http://localhost:15672 (guest/guest)
```

> Para subir a stack completa (todos os serviços + RabbitMQ), use o repositório
> [`fcg-orchestration`](../fcg-orchestration/) (`docker compose up -d --build`).

---

## Estrutura de Pastas

```
fcg-catalog-service/
├── nuget.config                          # feed GitHub Packages (FCGames.IntegrationEvents)
├── Dockerfile / Dockerfile.worker        # imagens da API e do Worker
├── app/src/
│   ├── Fiap.FCGames.Catalogo.Api/
│   │   ├── Controllers/                   # JogosController, ComprasController, BibliotecaController
│   │   └── Program.cs
│   ├── Fiap.FCGames.Catalogo.Worker/
│   │   ├── Consumers/                     # UsuarioCriadoEventoConsumer, PagamentoProcessadoEventoConsumer
│   │   └── Program.cs
│   ├── Fiap.FCGames.Catalogo.Application/ # Commands (RealizarCompra, jogos) + Queries (CQRS/MediatR)
│   ├── Fiap.FCGames.Catalogo.Domain/      # Jogo, Biblioteca, ItemBiblioteca, Pedido, StatusPedido
│   ├── Fiap.FCGames.Catalogo.Infra/       # FcGamesContexto, repositories, Migrations
│   └── Fiap.FCGames.Catalogo.CrossCutting/# Extensions (JWT, Swagger, Serilog, MassTransit)
└── docs/
```

---

## Checklist de Entrega

- [x] Domínio: `Jogo`, `Biblioteca`, `ItemBiblioteca`, `Pedido` + `StatusPedido`
- [x] EF Core migrations (catalog-db)
- [x] CRUD de jogos (`GET` público autenticado; `POST/PUT/DELETE` [Authorize Admin])
- [x] `POST /compras` (202) + `GET /compras/{orderId}` + `GET /biblioteca/{usuarioId}`
- [x] `GET /health`
- [x] Consumer `UsuarioCriadoEvento` (cria biblioteca, idempotente)
- [x] Consumer `PagamentoProcessadoEvento` (atualiza pedido + biblioteca)
- [x] Publisher `PedidoRealizadoEvento`
- [x] Transactional Outbox + Inbox (MassTransit) — entrega confiável
- [x] Projeto Worker separado
- [x] `nuget.config` (GitHub Packages)
- [x] Dockerfile multi-stage (API + Worker)
- [ ] Manifests Kubernetes (`/k8s/`)
