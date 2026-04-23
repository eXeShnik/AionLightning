# M4 — Chat server

Status: `[ ]` not started · Requires: M3 · ADRs: 001, 002, 003, 004

## Goal

A working .NET Chat server that registers with GS and routes the core chat channels between clients.

## Scope

### In

- `AionLightning.Chat` as a `BackgroundService` with:
  - Chat↔GS link (Chat connects to GS as a client; GS treats Chat as a registered service).
  - Chat↔Client listener on the configured port.
- Core chat packets (verify exact opcodes at milestone start from `AL-Chat/src/com/aionemu/chatserver/network/`):
  - `/shout` (shout chat).
  - `/whisper` (private message).
  - Party / group chat.
  - Legion chat.
- Minimal chat DAOs if the Java original has any in 4.6.0: banned words, mute history. Verify first; may be empty.
- Chat options record.

### Out of scope

- Global chat policies beyond what the Java original did.
- Rate limiting beyond Java's behaviour.
- Any client packet unrelated to chat.

## Dependencies

- Previous: M1, M2, M3.
- ADRs: 001, 002, 003, 004.

## Java source inventory

```bash
find AL-Chat/src -name "*.java" | wc -l
ls AL-Chat/src/com/aionemu/chatserver/network/chatserver/clientpackets/*.java
ls AL-Chat/src/com/aionemu/chatserver/network/chatserver/serverpackets/*.java
ls AL-Chat/src/com/aionemu/chatserver/network/gameserver/*.java
ls AL-Chat/src/com/aionemu/chatserver/model/*.java
```

Expect Chat to be the smallest of the three servers.

## .NET target layout

### New

- `AionLightning.Chat/ChatServerHost.cs` — `BackgroundService`.
- `AionLightning.Chat/Network/GameServer/GsConnection.cs` and factory — Chat as client of GS.
- `AionLightning.Chat/Network/GameServer/ClientPackets/*.cs`, `ServerPackets/*.cs`.
- `AionLightning.Chat/Network/Client/ClientConnection.cs` and factory — Chat accepts player clients.
- `AionLightning.Chat/Network/Client/ClientPackets/*.cs`, `ServerPackets/*.cs`.
- `AionLightning.Chat/Configs/Options/ChatOptions.cs`, `GameServerEndpointOptions.cs`.
- `AionLightning.Chat/Controller/ChatController.cs` (routes messages by channel).
- `AionLightning.Chat/Dao/*` — only if Java original has 4.6.0 chat DAOs.

### GS side additions

- `AionLightning.Game/Network/ChatServer/*` — the GS peer of the Chat link.

## Definition of Done

- [ ] Chat registers with GS; GS log shows "Chat registered".
- [ ] Two clients log in (via Login → GS), enter the world (stub from M3 extended to accept session, keep connection open with a placeholder "idle" state), bind to the same faction/area.
- [ ] Client A `/shout hello`. Client B sees the shout.
- [ ] Client A `/whisper ClientB msg`. Client B sees the whisper.
- [ ] Party / legion chat flows when a party / legion exists (stubbed group formation for this milestone).
- [ ] Shutting down Chat → GS logs the disconnect; `/shout` gracefully fails on the client (no crash).

## Smoke scenario

1. Start Login + GS + Chat.
2. Start two Aion clients, log in as two different accounts.
3. Both clients enter the world (stub, M5 will make this real).
4. Client A runs `/shout hello world`. Verify Client B sees it.
5. Client A whispers Client B. Verify delivery.
6. Kill Chat. Verify graceful failure path.

## Scoped risks

- **World-entry stub dependency.** M4 needs clients to be "in the world" to route chat. Building a full world entry is M5; M4 relies on an M3-extended stub that keeps the GS client connection open without running the full world. Keep the stub tight — avoid growing it into a partial M5.
- **Chat packets depend on character identity.** The stub must at minimum expose `AccountId`, `PlayerId`, `Name`, `Faction` to Chat. Verify these fields are already sent from GS to Chat in the Java protocol.
- **Charset.** Verify the encoding used for chat message strings on the wire (UTF-16 LE per Aion spec; watch for locale-specific cases on older 4.6.0 builds).

## Notes

- Chat is a good milestone to validate the M1 networking base under two independent listeners in one process.
- If the Java 4.6.0 Chat source is empty on some packets (reserved / unused), do not port them; track with a `// TODO (R-006): reserved in 4.6.0` comment.
