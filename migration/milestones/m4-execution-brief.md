# M4 — Execution brief

Scope in [`m4-chat-server.md`](m4-chat-server.md). **Read [`../agent-rules.md`](../agent-rules.md) first.**

## Preconditions

- M3 closed.
- `/tmp/aion-refs/4.6.2/AL-Chat/` accessible.
- World-entry stub on GS: GS accepts post-handoff client, keeps the connection open without running world logic, exposes `AccountId`, `PlayerId`, `Name`, `Faction` to Chat via the Chat↔GS channel.

## Reading scope (for agent — see [`../agent-prompt.md`](../agent-prompt.md) tiers)

**Tier A (mandatory):** `agent-rules.md`, `conventions.md`, `m4-chat-server.md`, this file.

**Tier B — must-read ADRs for M4:**
- [`../adr/001-networking.md`](../adr/001-networking.md) — two listeners on Chat (Chat↔GS and Chat↔Client).
- [`../adr/002-configuration.md`](../adr/002-configuration.md) — `ChatOptions`, `GameServerEndpointOptions`.
- [`../adr/003-hosting.md`](../adr/003-hosting.md) — `ChatServerHost` pattern.
- [`../adr/004-database.md`](../adr/004-database.md) — open only if the Java 4.6.2 Chat source has DAOs. Verify at inventory time; if no DAOs, skip.

**Tier B — step-triggered opens:**
- [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md) — when porting chat packet classes.

**Do NOT read:** `adr/005-callbacks.md`, `adr/006-scripting.md`, `adr/007-schema-migration.md`, `data-schema.md` (usually) — not needed.

**Tier C — on-demand:**
- [`../risks.md`](../risks.md) — R-001 for packet ports.
- [`../glossary.md`](../glossary.md) — as needed.

## Packet inventory

Agents must run this at milestone start:

```bash
ls /tmp/aion-refs/4.6.2/AL-Chat/src/com/aionemu/chatserver/network/chatserver/clientpackets/
ls /tmp/aion-refs/4.6.2/AL-Chat/src/com/aionemu/chatserver/network/chatserver/serverpackets/
ls /tmp/aion-refs/4.6.2/AL-Chat/src/com/aionemu/chatserver/network/gameserver/
find /tmp/aion-refs/4.6.2/AL-Chat/src -name "*PacketHandlerFactory*.java" -exec cat {} \;
```

Copy the opcode → class mapping from the handler factory into `milestones/m4-packet-inventory.md` (new file created at M4 start) in the same table format as M2/M3.

## Scope summary (repeats scope file)

- `AionLightning.Chat` → `ChatServerHost : BackgroundService` with two listeners:
  - Outbound to GS (`Chat` side of Chat↔GS link).
  - Inbound from Aion client on Chat-specific port.
- GS side: `AionLightning.Game/Network/ChatServer/*` — Chat↔GS counterpart on GS (pass player ↔ chat-server context).
- Chat controller: route messages by channel (`SHOUT`, `WHISPER`, `PARTY`, `GROUP`, `LEGION`).
- Chat DAO — only if 4.6.2 Java has any (check at inventory time).

## Port order

1. Options: `ChatOptions`, `GameServerEndpointOptions` on Chat side.
2. Chat↔GS packet classes (outbound + inbound) — copy names from `AL-Chat/src/.../network/gameserver/`.
3. `ChatServerHost` with GS connect logic, reconnect-on-drop with backoff (mirror of GS-reconnect-to-LS from M3).
4. Chat client listener — Aion client connects to Chat after GS hands it off (protocol detail: GS tells the client the Chat endpoint).
5. Chat client packet pipeline, base classes (reuse M1 Commons).
6. Player-session model on Chat side: a map `accountId → PlayerChatSession { Name, Faction, Channel[]Subscriptions }`.
7. Channel handlers: shout broadcast (same map + region filter), whisper (target lookup), party/group/legion (member-list lookup).
8. GS-side: send player-context packets to Chat on login / logout / zone-change.

## Smoke steps

1. Start LS + GS + Chat.
2. Start two Aion clients on different accounts; log in both, land in the world stub.
3. Client A: `/shout hello world`. Client B sees it.
4. Client A: `/whisper ClientB msg`. Client B sees it.
5. Form party (world stub must expose party formation — if not, scope shift: add a minimal `PARTY_CREATE` handler in world stub for M4 only).
6. Party chat: Client A `/p hi team`. Client B sees it, Client C (outside party) does not.
7. Kill Chat. `/shout` gracefully fails on clients (no crash).
8. Restart Chat. Normal chat resumes.

## Exit criteria

- All DoD items in [`m4-chat-server.md`](m4-chat-server.md) ticked.
- Journal entry: `M4 closed: chat core live`.
- Active pointer → M5.

## Scoped risk

- **World stub growth.** Party formation for `/p` chat test may require extending the world stub beyond what M3 landed. Keep the stub additions minimal and tagged `// TEMP (M4): remove during M5 world entry`.

## References

- Java: `/tmp/aion-refs/4.6.2/AL-Chat/src/com/aionemu/chatserver/`.
- Java GS side of Chat link: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/network/chatserver/`.
