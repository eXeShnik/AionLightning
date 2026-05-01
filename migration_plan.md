# AionLightning Migration Plan

## **Current repo based has many server versions, our migration based on 4.6.0**

## **Main thing I want u to save your history, all important doings or things**

## Before implementing u should check plan, then split it by smaller tasks and save near project, then assign them to another agents

## Java project structure

- `Tools` folder we can skip
- Project has been written with old Java 1.7, so I think some libs and classes are unnecessary for latest net 10, so u should analyses migrated code, find them then save it as important thing, so u will know later what we do not need to rewrite and use it from net 10
- Java project folder we need to care about them - `src/` and `libs/`, `data/`
- `AL-Commons` - shared lib, here we have common classes which uses in other projects
- `AL-Chat` - console app, uses for chat packets, communicating with client and game server
- `AL-Login` - console app, uses for waiting login packets from game then make connection from client to game server
- `AL-Game` - console app, uses for main game packets, working with skills, quest, other things related to game

## Project Goals

- Rewrite the 4 modules (Commons - Shared, Login, Chat, Game) to .NET 10 console apps.
- Use .NET Host builder for application lifecycle management.

## Architectural Decisions

- **Configuration** Use `IConfiguration` with JSON files for configuration.
- **Application architecture** Use Dependency Injection for services.
- **Logging**: Use `Microsoft.Extensions.Logging.ILogger<T>` injected via constructors instead of a static logger factory. For logging we use `Serilog`
- **Cryptography**: Use the `BouncyCastle.Cryptography` library for all cryptographic operations.
- **Database**: Use `MySql.Data` for MySQL database connectivity.
- **Networking**: Port the custom Java NIO networking layer to a .NET equivalent using `System.Net.Sockets.TcpClient`.
- **AOP/Callbacks**: The Java Agent-based AOP for callbacks will be provisionally migrated with placeholder classes. A full implementation will require a dedicated .NET AOP library like Castle.Core or source generators. ??? I think we need to rethink this part, we might not need it

## Rework current migrated things

- First of all before moving forward, we need to replace simple `Socket` implementation with `TcpClient` and `TcpListener`
- Regarding scripting I added `CSharpCompilerService` what I am thinking about that, we need to listen `Scripts` folder in corresponding app (Login, Game, Chat), I added `FolderListenerService` to listen folder changes, so we can listen then load and cache script into memory, so important thing that all scripts will be use only existing classes which we have in Common or any others project

## Migration Steps

1. [✓] Set up solution and project structure for .NET 10.
2. [✓] Implement `ILogger` with console output for all projects.
3. [✓] Refactor logging to use constructor injection.
4. [✓] Add `BouncyCastle.Cryptography` package.
5. [✓] Migrate `AL-Commons` module.
   - [✓] `utils`
   - [✓] `callbacks`
   - [✓] `configuration`
   - [✓] `database`
   - [✓] `network`
   - [✓] `objects`
   - [✓] `options`
   - [✓] `scripting`
   - [✓] `services` (CronService/Quartz, ScriptService/Roslyn, FolderListenerService)
   - [✓] `taskmanager` (AbstractLockManager → ReaderWriterLockSlim)
   - [✓] `versionning` (Version reads from AssemblyInformationalVersionAttribute; Locator is JAR-specific, not ported)
6. [✓] Migrate `AL-Login` module.
   - [✓] Network layer (TcpListener/TcpClient), packet framing, blowfish+RSA crypto
   - [✓] Auth flow: CM_LOGIN, CM_PLAY, CM_SERVER_LIST, CM_AUTH_GG, CM_UPDATE_SESSION
   - [✓] DAO layer: AccountDao, BannedIpDao backed by MySql.Data
   - [✓] GameServer registry: GameServerTable, GameServerInfo, GS↔LS connection
   - [✓] DI wiring in Program.cs; appsettings.json configuration
7. [✓] Migrate `AL-Chat` module. (M4 — 2026-04-29)
   - [✓] Chat server project setup (csproj, appsettings.json, options)
   - [✓] GS↔Chat listener (port 9021): CM_CS_AUTH, CM_PLAYER_AUTH, CM_PLAYER_LOGOUT, CM_PLAYER_GAG
   - [✓] Aion client↔Chat listener (port 10241): CM_CHAT_INI, CM_PLAYER_AUTH, CM_CHANNEL_REQUEST, CM_CHANNEL_MESSAGE
   - [✓] ChatService: token generation (SHA-256), dynamic channel registry, rate-limiting, gag
   - [✓] SM_GS_AUTH_RESPONSE, SM_PLAYER_AUTH_RESPONSE, SM_CHANNEL_RESPONSE, SM_CHANNEL_MESSAGE
   - [✓] GS-side CS connection (CsConnection, CsConnectionHolder, reconnect loop in GameServerHost)
   - [✓] CM_CHAT_AUTH (0x14C) in GsPacketHandlerFactory; SM_CHAT_INIT (0xE6) to Aion client
   - [✓] Player logout notification from GsClientConnection.DisposeAsync
8. [✓] Migrate `AL-Game` module.
   - [✓] Player model, DAO layer (player + appearance), movement + login packet handlers
   - [✓] World registry, event bus, scripting, LS/CS connections
   - [✓] Config options: WorldOptions, RateOptions, GsOptions (IOptions<T> DI pattern)
   - [✓] DataManager + XML loading infrastructure (IDataManager DI singleton)
   - [✓] PlayerStatsData — loads per-class XML templates from data/static_data/stats/player/, post-load math (HP/MP/evasion/block/parry recalc)
   - [✓] PlayerInitialData — loads spawn locations from player_initial_data.xml
   - [✓] PlayerStatsTemplate + StatsTemplate + CreatureSpeeds model hierarchy
   - [✓] CM_ENTER_WORLD wired to DataManager — real HP/MP from class+level template
   - [✓] SM_STATS_INFO uses real attributes (power/health/agility/accuracy/knowledge/will, evasion/block/parry, combat stats) from template
   - [✓] Skill data + PlayerSkillList (template lookup + basic cooldown)
   - [✓] NPC template + basic spawning (NpcData, SpawnsData, Npc model, SpawnService, World NPC registry)
   - [✓] PlayerExperienceTable, XP gain, level-up handling (ExperienceService, SM_STATUPDATE_EXP, SM_LEVEL_UPDATE)
   - [✓] Item system, inventory model, starting items from PlayerInitialData (ItemData, Item, PlayerInventory, IItemDao, V2__items.sql migration)
9. [✓] Post-migration gap fixes (session 2026-04-29)
   - [✓] V3__add_level_column.sql — `players.level` was missing from V1 schema; DAO reads/writes it
   - [✓] V4__player_id_autoincrement.sql — `players.id` made AUTO_INCREMENT for character creation
   - [✓] `IPlayerDao.UpdateExpLevelAsync` + `PlayerDaoImpl` — saves EXP+Level on logout (was never persisted)
   - [✓] `IPlayerDao.MarkDeletedAsync` — marks character for deletion (7-day grace period)
   - [✓] `GsClientConnection.DisposeAsync` now calls `UpdateExpLevelAsync` + `IItemDao.SaveAllAsync` on disconnect
   - [✓] `IItemDao` injected into `GsClientConnection` + `GsConnectionFactory`
   - [✓] `CM_CREATE_CHARACTER` (0x175) + `SM_CREATE_CHARACTER` (0xC9) — full character creation flow
   - [✓] `CM_DELETE_CHARACTER` (0x17A) + `SM_DELETE_CHARACTER` (0xCA) — character deletion with 7-day grace period
   - [✓] `PlayerClassExtensions.IsStartingClass()` + `FromId()` helpers
   - [✓] `SM_CHARACTER_LIST.WritePlayerInfo` made `internal static` for reuse by `SM_CREATE_CHARACTER`
   - [✓] `SM_INVENTORY_INFO` (0x1A) — minimal GENERAL_INFO blob per item; sent in CM_ENTER_WORLD (first + sentinel)
   - [✓] `SM_NPC_INFO` (0x0E) — introduces spawned NPCs to entering player in CM_LEVEL_READY
   - [✓] `V4__player_id_autoincrement.sql` — players.id AUTO_INCREMENT; InsertAsync no longer passes explicit id
   - [✓] `CM_CHECK_NICKNAME` (0x173) + `SM_NICKNAME_CHECK_RESPONSE` (0xE9) — name availability check
   - [✓] `CM_RESTORE_CHARACTER` (0x17B) + `SM_RESTORE_CHARACTER` (0xCB) — cancel 7-day deletion
   - [✓] `IPlayerDao.CancelDeletionAsync(playerId, accountId)` — ownership-validated SQL update
   - [✓] `EmotionType` enum, `CreatureState` flags enum — creature state model
   - [✓] `Creature.State`, `Creature.Target`, `Creature.MovementSpeed` — targeting + emotion state
   - [✓] `Npc.Level` property (proxies Template.Level); `Npc` constructor sets Name from template
   - [✓] `SM_EMOTION` (0x25) — full per-emotion-type write switch matching Java
   - [✓] `CM_EMOTION` (0xC9) — state updates (RESTING/WALKING/WEAPON_EQUIPPED/FLYING/etc) + broadcast
   - [✓] `SM_TARGET_SELECTED` (0x29) — target level/HP/MP to selecting player
   - [✓] `SM_TARGET_UPDATE` (0x51) — broadcast player's new target objectId to others
   - [✓] `CM_TARGET_SELECT` (0xFD) — look up player or NPC in world, set Target, send responses
   - [✓] `CM_SUBZONE_CHANGE` (0x161) — reads 1 byte, no response (zone system not yet implemented)
   - [✓] Character list deletion fix: `FindByAccountIdAsync` removes `deletion_date IS NULL` filter; pending-deletion chars visible with timer
   - [✓] `Player.DeletionDate` + `Player.DeletionTime` — deletion timestamp surfaced to packet layer
   - [✓] `SM_CHARACTER_LIST.WritePlayerInfo` items format fix — 208-byte slot buffer + D(deletionTime) replacing wrong WriteH(0)
10. [✓] Combat, movement, and inventory packets (session 2026-04-29)
    - [✓] `Creature.IsAlreadyDead`, `Creature.HpPercentage` — death/health state helpers
    - [✓] `SM_ATTACK_STATUS` (0x05) — damage/heal with TYPE and LOG enums matching Java exactly
    - [✓] `SM_DIE` (0xC1) — death notification with revive option flags
    - [✓] `SM_CASTSPELL` (0x21) — skill-cast animation, all targetType variants
    - [✓] `SM_DELETE_ITEM` (0x1C) — tells client to remove item from inventory display
    - [✓] `CM_ATTACK` (0xE2) — placeholder damage (level×6+rand), SM_ATTACK_STATUS broadcast, SM_DIE on kill
    - [✓] `CM_CASTSPELL` (0xE3) — validates skill known, broadcasts SM_CASTSPELL animation
    - [✓] `CM_MOVE_IN_AIR` (0xF3) — fly-teleport position update, no broadcast
    - [✓] `CM_REVIVE` (0xA7) — restores 25% HP/MP, clears Dead state, broadcasts SM_EMOTION(RESURRECT), re-sends SM_STATS_INFO
    - [✓] `CM_DELETE_ITEM` (0x156) — removes from inventory + DB, sends SM_DELETE_ITEM
    - [✓] `CM_DIALOG_SELECT` (0x114) — stub (quest engine not yet implemented)
    - [✓] `CM_EQUIP_ITEM` (0xC4) — fully implemented (see item 11)
    - [✓] `CM_MOVE_ITEM` (0x17E) — stub (warehouse not yet implemented)
    - [✓] `CM_USE_ITEM` (0xC7) — stub (item effect system not yet implemented)
    - [✓] `IItemDao.DeleteAsync` + `ItemDaoImpl` — single-item DELETE by unique_id
11. [✓] Enter-world sequence, NPC lifecycle, and combat polish (session 2026-04-30)
    - [✓] `AionServerPacket.Opcode` widened from `byte` to `ushort` — supports 2-byte opcodes (e.g. 0x124, 0x10A)
    - [✓] `GsCrypt.EncodeOpcode` updated to accept `ushort`; all non-GS connection send paths cast to `(byte)` for byte-range opcodes
    - [✓] `SM_FRIEND_LIST` (0x84) — empty friend list header
    - [✓] `SM_BLOCK_LIST` (0xE0) — empty block list header
    - [✓] `SM_SKILL_COOLDOWN` (0x33) — empty cooldown list (0 entries, flag=1)
    - [✓] `SM_ABNORMAL_EFFECT` (0x32) — clear all buffs/debuffs for a creature (count=0, type=1 creature / 2 player)
    - [✓] `SM_PACKAGE_INFO_NOTIFY` (0x10A) — in-game shop package count (ushort opcode)
    - [✓] `SM_ATTACK` (0x36) — auto-attack animation broadcast (attacker/target IDs, HP%, single NORMALHIT entry)
    - [✓] `SM_DELETE` (0x16) — object despawn packet (objectId + removal speed byte)
    - [✓] `CM_ENTER_WORLD` — full Java `PlayerEnterWorldService.enterWorld()` ordering: SM_CHARACTER_SELECT → SM_SKILL_LIST → SM_SKILL_COOLDOWN → SM_QUEST_COMPLETED_LIST → SM_QUEST_LIST → SM_TITLE_INFO×2 → SM_MOTION → SM_ENTER_WORLD_CHECK → inventory/stats/cube → SM_INSTANCE_INFO → SM_CHANNEL_INFO → SM_PLAYER_SPAWN → SM_GAME_TIME → SM_TITLE_INFO(list) → SM_EMOTION_LIST → SM_PRICES → SM_ABYSS_RANK → SM_PACKAGE_INFO_NOTIFY → SM_MACRO_LIST → SM_RECIPE_LIST
    - [✓] `CM_LEVEL_READY` — broadcasts `SM_MOTION.Broadcast` + `SM_ABNORMAL_EFFECT` (clear) for entering player to all others
    - [✓] `CM_ATTACK` — sends `SM_ATTACK` animation before `SM_ATTACK_STATUS`; NPC death: SM_EMOTION(DIE) → world.Remove → XP award → delayed SM_DELETE (3 s) → ScheduleRespawn
    - [✓] `CM_SHOW_FRIENDLIST` (0x184) — now sends `SM_FRIEND_LIST` (was no-op stub)
    - [✓] `CM_SHOW_BLOCKLIST` (0x17C) — now sends `SM_BLOCK_LIST` (was no-op stub)
    - [✓] `ExperienceService` wired into `GsPacketHandlerFactory` → `CM_ATTACK` (NPC kills grant XP)
    - [✓] `SpawnService` — added `ScheduleRespawn(npc, delaySeconds=30)`: re-creates NPC, adds to world, broadcasts `SM_NPC_INFO` to all online players
    - [✓] `SpawnService` injected into `GsPacketHandlerFactory` → `CM_ATTACK`
    - [✓] `PlayerConnectionRegistry` — `GetAll()` already present; now used by SpawnService respawn broadcast
    - [✓] `LootService` — in-memory `ConcurrentDictionary<int, List<LootEntry>>` keyed by NPC objectId; `GenerateDrops(npc)` drops kinah + 50% Minor Life Elixir; `TakeLootAt` / `GetLoot` / `ClearLoot`
    - [✓] `SM_INVENTORY_ADD_ITEM` (0x1B) — notifies client of newly acquired items using same GENERAL_INFO blob as SM_INVENTORY_INFO
    - [✓] `SM_UPDATE_PLAYER_APPEARANCE` (0x24) — broadcasts equipment visual state (slot mask + per-slot itemId/godstone/color/enchant) to players in range
    - [✓] `CM_START_LOOT` (0x178) — opens loot window: sends SM_LOOT_STATUS(Open) + SM_LOOT_ITEMLIST with real drop data from LootService
    - [✓] `CM_LOOT_ITEM` (0x179) — takes item at index; kinah stacks into existing kinah item; regular items find/create stack; saves to DB; sends SM_INVENTORY_ADD_ITEM; closes loot window when empty
    - [✓] `CM_EQUIP_ITEM` (0xC4) — equip (action=0): item.Slot = slotMask; unequip (action=1): item.Slot = -1; saves to DB; sends SM_INVENTORY_ADD_ITEM to self + broadcasts SM_UPDATE_PLAYER_APPEARANCE to all
    - [✓] `PlayerInventory.FindByItemId` — lookup by itemId for kinah stacking
    - [✓] `LootService` + `SM_INVENTORY_ADD_ITEM` registered/added to `Program.cs` and `GsPacketHandlerFactory`
12. [✓] Item use, NPC shops, and item data extensions (session 2026-04-30)
    - [✓] `ItemTemplate` — added `<actions><skilluse skillid="N"/>` XML parsing; exposes `UseSkillId` (nullable int)
    - [✓] `ShopData` — loads `goodslists/goodslists.xml` (goodslist id → item IDs) + `npc_trade_list.xml` (NPC id → goodslist IDs); `NpcSellsItem(npcId, itemId)` and `GetItemsForNpc(npcId)` queries
    - [✓] `IDataManager` + `DataManager` — added `ShopData Shop` property; loaded after `ItemData`
    - [✓] `CM_USE_ITEM` (0xC7) — reads uniqueItemId + type; looks up `UseSkillId` from item template; applies HP/MP restore from hardcoded skill-ID table (skills 10202–10208 = HP, 10262–10268 = MP, 15–45% per tier); decrements count, removes from DB+inventory if exhausted, sends `SM_ATTACK_STATUS` (heal) + `SM_STATS_INFO` + `SM_INVENTORY_ADD_ITEM` or `SM_DELETE_ITEM`
    - [✓] `CM_BUY_ITEM` (0xF1) — tradeActionId=13 (buy from shop): verifies item in NPC's goods list, checks kinah, deducts kinah, creates/stacks item, saves, sends `SM_INVENTORY_ADD_ITEM`; tradeActionId=1 (sell to shop): finds item by templateId, removes from inventory, gives 50% price in kinah, saves, sends `SM_DELETE_ITEM` + kinah update
    - [✓] `GsPacketHandlerFactory` — CM_USE_ITEM and CM_BUY_ITEM now fully wired with conn + itemDao + dataManager + world
13. [✓] NPC dialog, shop window, and inventory move (session 2026-04-30)
    - [✓] `ShopData` — updated to store goodslist IDs per NPC separately (for SM_TRADELIST tab order); `HasShop(npcId)` + `GetGoodsListIds(npcId)` added alongside existing `NpcSellsItem`
    - [✓] `SM_TRADELIST` (0x8F) — sends NPC shop to client: npcObjectId, type(0=NORMAL), sell/buy price rates (100), 4.6 flags, goodslist tab IDs, no limited items
    - [✓] `CM_SHOW_DIALOG` (0x116) — now looks up NPC in world; sends `SM_DIALOG_WINDOW(npcObj, 10)` greeting only if NPC exists
    - [✓] `CM_DIALOG_SELECT` (0x114) — handles dialogId=2 (BUY): sends `SM_TRADELIST` if NPC has goods lists, `SM_DIALOG_WINDOW(0)` otherwise; handles dialogId=103 (TRADE_SELL_LIST): sends `SM_DIALOG_WINDOW(21)` sell page; logs unhandled dialog IDs at Debug level
    - [✓] `CM_MOVE_ITEM` (0x17E) — inventory-to-inventory reorder (source=0, dest=0): updates Item.Slot to new bag position, saves to DB, sends `SM_INVENTORY_ADD_ITEM`; warehouse moves (source/dest≠0) are no-ops
    - [✓] `GsPacketHandlerFactory` — `ILoggerFactory` added; CM_SHOW_DIALOG, CM_DIALOG_SELECT, CM_MOVE_ITEM now fully wired
14. [✓] Stack split, skill effects, and teleport system (session 2026-04-30)
    - [✓] `SM_SKILL_ACTIVATION` (0x2E) — new packet: signals skill cast completion to all clients (skillId + isActive flag)
    - [✓] `CM_CASTSPELL` (0xE3) — upgraded from animation-only stub: broadcasts `SM_CASTSPELL`, checks SkillType (MAGICAL/PHYSICAL = damage), fires Task.Run with castDelay → broadcasts `SM_SKILL_ACTIVATION` → applies level-scaled damage (level×8+rand[20,60]) → `SM_ATTACK_STATUS(SpellAtk)` → full NPC death path (SM_EMOTION(DIE) → drops → XP → delayed SM_DELETE → ScheduleRespawn); non-damage skills get immediate activation broadcast
    - [✓] `CM_SPLIT_ITEM` (0x17F) — split stack to empty slot: creates new Item at slotNum with splitAmount, reduces source count, saves both, sends `SM_INVENTORY_ADD_ITEM`; merge with same-item stack: transfers splitAmount from source to target, saves both; warehouse moves no-op
    - [✓] `TeleportData` — loads `teleport_location.xml` (loc_id → mapId + coordinates) and `npc_teleporter.xml` (NPC IDs, space/comma-separated, → list of TeleDestination with loc_id + price + type); `IsTeleporter`, `GetDestinations`, `GetDestination`, `GetLocation` queries
    - [✓] `IDataManager` + `DataManager` — added `TeleportData Teleports` property; loaded after ShopData
    - [✓] `CM_TELEPORT_SELECT` (0x176) — looks up NPC in world → gets destination by locId → gets location coords → deducts kinah (saves+notifies client) → updates `player.Position` → sends `SM_TELEPORT_LOC`; locations without coordinates (FLIGHT-only) are ignored
    - [✓] `GsPacketHandlerFactory` — CM_CASTSPELL, CM_SPLIT_ITEM, CM_TELEPORT_SELECT now fully wired
15. [✓] Chat system, bind points, and social lists (session 2026-04-30)
    - [✓] `SM_MESSAGE` (0x18) — chat packet: ChatType enum (Normal/Shout/Whisper/Group/Legion); write varies by type (Normal: H+S, Shout: S+S+F+F+F, others: S+S)
    - [✓] `CM_CHAT_MESSAGE_PUBLIC` (0xF9) — broadcasts Normal/Shout to all connected players via `connRegistry.GetAll()`; Group/Legion are no-ops (pending party/guild)
    - [✓] `CM_CHAT_MESSAGE_WHISPER` (0xFE) — routes to target by name via `connRegistry.GetByName()`; echoes packet back to sender
    - [✓] `PlayerConnectionRegistry` — added `GetByName(name)` for whisper routing
    - [✓] `SM_BIND_POINT_INFO` (0xEB) — sends obelisk bind coordinates (worldId, x, y, z); kisk type=0, kisk objectId=0
    - [✓] `NpcTemplate` — added `[XmlAttribute("type")] NpcType` for bindstone detection
    - [✓] `CM_SHOW_DIALOG` (0x116) — detects `NpcType == "BINDSTONE"`, sets `player.BindPosition`, sends `SM_BIND_POINT_INFO`; all other NPCs send `SM_DIALOG_WINDOW(10)`
    - [✓] `Player.BindPosition` (`Position?`) — nullable bind point set on obelisk interaction
    - [✓] `CM_REVIVE` (0xA7) — if bind position is set, sends `SM_TELEPORT_LOC` to bind coordinates before resurrection broadcast
    - [✓] `V5__social_lists.sql` — `friend_list` (player_id, friend_id, note) + `block_list` (player_id, blocked_id, reason) with FK to players
    - [✓] `FriendEntry` + `BlockEntry` model records in `Model/Social/`
    - [✓] `ISocialDao` + `SocialDaoImpl` — friend/block CRUD with player-info JOIN for name/class/level/race
    - [✓] `IPlayerDao.FindByNameAsync` + `PlayerDaoImpl` — lookup by name for friend/block add flows
    - [✓] `SM_FRIEND_LIST` (0x84) — updated to accept real `IReadOnlyList<FriendEntry>` + `HashSet<int> onlineIds`; per-entry: objectId/name/class/level/race/online flag/mutual(0)/note
    - [✓] `SM_BLOCK_LIST` (0xE0) — updated to accept real `IReadOnlyList<BlockEntry>`; per-entry: objectId/name/reason
    - [✓] `SM_FRIEND_RESPONSE` (0x12E) — result of friend add/remove; ResultCode enum: Added/AlreadyFriends/LimitReached/Blocked/NotFound/Removed
    - [✓] `CM_FRIEND_ADD` (0x10D) — DB lookup by name, duplicate check, `ISocialDao.AddFriendAsync`, sends `SM_FRIEND_RESPONSE(Added/AlreadyFriends/NotFound)`
    - [✓] `CM_FRIEND_DEL` (0x132) — reads objectId, removes from DB, sends `SM_FRIEND_RESPONSE(Removed)`
    - [✓] `CM_FRIEND_STATUS` (0x148) — reads status byte; refreshes full friend list to reflect online flags
    - [✓] `CM_BLOCK_ADD` (0x144) — lookup by name, `ISocialDao.AddBlockAsync`, refreshes `SM_BLOCK_LIST`
    - [✓] `CM_BLOCK_DEL` (0x145) — removes block by objectId, refreshes `SM_BLOCK_LIST`
    - [✓] `CM_BLOCK_SET_REASON` (0x171) — updates block note in DB (no response needed)
    - [✓] `CM_SHOW_FRIENDLIST` (0x184) — now loads real friends from DB + online set from connRegistry
    - [✓] `CM_SHOW_BLOCKLIST` (0x17C) — now loads real blocks from DB
    - [✓] `CM_GM_COMMAND_SEND` (0xC8) — parses dot-prefix admin commands: `.heal` (full HP/MP restore), `.level N` (set level 1-65 with XP/stats update), `.tp mapId x y z` (teleport), `.item itemId [count]` (give item, stacks kinah)
    - [✓] `ISocialDao` registered in `Program.cs`; GsPacketHandlerFactory wired for all social + GM packets
16. [✓] Mail system and player-to-player exchange (session 2026-04-30)
    - [✓] `V6__mail.sql` — `mail` table: sender/receiver, title, message, attached_item_id, attached_count, attached_kinah, letter_type, is_read, attachment_taken, recipient_deleted
    - [✓] `MailEntry.cs` — mutable class (not record) in `Model/Mail/`; IsRead + AttachmentTaken modified on claim
    - [✓] `IMailDao` + `MailDaoImpl` — GetReceivedMailsAsync, InsertAsync, MarkReadAsync, DeleteAsync (soft-delete via recipient_deleted), MarkAttachmentTakenAsync (zeros attached fields)
    - [✓] `SM_MAIL_SERVICE` (0xA1) — single packet class, 6 serviceId modes: MailboxState(0), SendResult(1), LetterList(2), LetterContent(3), AttachmentTaken(5), LetterDeleted(6)
    - [✓] `CM_READ_MAIL` (0x124) — marks mail read, sends LetterContent response
    - [✓] `CM_SEND_MAIL` (0x126) — validates receiver, inserts mail (with optional kinah/item attachment), sends SendResult; notifies receiver if online via MailboxState update
    - [✓] `CM_DELETE_MAIL` (0x12B) — soft-deletes mail, sends LetterDeleted response
    - [✓] `CM_GET_MAIL_ATTACHMENT` (0x12A) — claims attachment (kinah or item), creates item with NextUniqueIdAsync, sends AttachmentTaken + SM_INVENTORY_ADD_ITEM
    - [✓] `CM_ENTER_WORLD` updated — sends SM_MAIL_SERVICE(MailboxState) with total/unread counts at login
    - [✓] `ExchangeService` — ConcurrentDictionary<int, ExchangeSession> keyed by each participant objectId; Start/Cancel/Complete lifecycle
    - [✓] `ExchangeSession` — Initiator/Target players, item lists, kinah amounts, locked flags, BothLocked helper
    - [✓] `SM_EXCHANGE_REQUEST` (0x4A) — sends partner name to both players
    - [✓] `SM_EXCHANGE_ADD_ITEM` (0x4B) — action(0=self,1=partner), item GENERAL_INFO blob
    - [✓] `SM_EXCHANGE_ADD_KINAH` (0x4D) — action(0=self,1=partner), kinah amount
    - [✓] `SM_EXCHANGE_CONFIRMATION` (0x4E) — Action enum: Opened(8)/PartnerLocked(1)/Success(3)/Cancelled(4)
    - [✓] `CM_EXCHANGE_REQUEST` (0x11D) — auto-starts exchange session (no question window); sends SM_EXCHANGE_REQUEST to both
    - [✓] `CM_EXCHANGE_ADD_ITEM` (0x102) — adds item from initiator's inventory to session; broadcasts SM_EXCHANGE_ADD_ITEM
    - [✓] `CM_EXCHANGE_ADD_KINAH` (0x100) — validates kinah balance, updates session; broadcasts SM_EXCHANGE_ADD_KINAH
    - [✓] `CM_EXCHANGE_LOCK` (0x101) — sets locked flag; when both locked: ExecuteTradeAsync (swap items + kinah, SaveAllAsync both, Complete, send Success); else send PartnerLocked
    - [✓] `CM_EXCHANGE_OK` (0x2E6) — delegates to CM_EXCHANGE_LOCK (second lock confirmation)
    - [✓] `CM_EXCHANGE_CANCEL` (0x2E7) — cancels session, sends Cancelled to both
    - [✓] `IMailDao` + `ExchangeService` + `ISocialDao` registered in `Program.cs`
17. [✓] Party/Group system (session 2026-04-30)
    - [✓] `PlayerGroup` model (`Model/Group/PlayerGroup.cs`) — groupId, members (max 6), leaderObjectId, lootDistribution, AddMember/RemoveMember/IsLeader/HasMember
    - [✓] `GroupService` — ConcurrentDictionary keyed by groupId and by memberId; CreateGroup/JoinGroup/LeaveGroup; LeaveGroup auto-disbands when < 2 members remain
    - [✓] `Player.Group` (`PlayerGroup?`) — nullable property set/cleared by GroupService
    - [✓] `SM_GROUP_INFO` (0x43) — full group member list: groupId, count, loot settings, per-member: objectId/name/class/race/level/HP/MP/leaderFlag/status/position; 0-member variant signals dissolution
    - [✓] `SM_GROUP_MEMBER_INFO` (0x6C) — per-member HP/MP/status update: groupId, objectId, HP, MP, level, status, position
    - [✓] `CM_INVITE_TO_GROUP` (0x123) — reads target name; auto-accepts (no question window); creates or joins group via GroupService; broadcasts SM_GROUP_INFO to all members
    - [✓] `CM_GROUP_DISTRIBUTION` (0x10E) — leader changes loot settings; broadcasts SM_GROUP_INFO with updated settings
    - [✓] `GsClientConnection.DisposeAsync` — calls GroupService.LeaveGroup on disconnect; broadcasts updated SM_GROUP_INFO to remaining members (or dissolution packet if group disbanded)
    - [✓] `RegenService` — after HP/MP tick, broadcasts SM_GROUP_MEMBER_INFO to all other group members
    - [✓] `GroupService` registered in `Program.cs`; GsConnectionFactory + GsPacketHandlerFactory wired with GroupService
18. [✓] Warehouse, group XP sharing, and enchanting (session 2026-04-30)
    - [✓] `V7__enchant_warehouse.sql` — adds `storage_type` (0=inventory, 1=warehouse) and `enchant_level` (0-15) columns to `player_items`; adds composite index on (player_id, storage_type)
    - [✓] `Item.StorageType` (byte) + `Item.EnchantLevel` (byte) — new model fields
    - [✓] `Item.IsEquipped` — updated to require `StorageType == 0` (warehouse items can never be "equipped")
    - [✓] `IItemDao.FindWarehouseItemsAsync` + `SaveWarehouseAsync` — load/save personal warehouse items (storage_type=1)
    - [✓] `ItemDaoImpl.SaveAllAsync` — now scopes DELETE to storage_type=0 (no longer deletes warehouse items on inventory save); `SaveWarehouseAsync` scopes to storage_type=1
    - [✓] `ItemDaoImpl` — all queries load/persist `storage_type` and `enchant_level` columns
    - [✓] `Player.Warehouse` (`PlayerInventory`) — personal warehouse collection on Player
    - [✓] `CM_ENTER_WORLD` — loads personal warehouse items from DB into `player.Warehouse`
    - [✓] `GsClientConnection.DisposeAsync` — persists warehouse items on disconnect (`SaveWarehouseAsync`)
    - [✓] `SM_WAREHOUSE_INFO` (opcode 0x1A, type byte 2) — personal warehouse packet; same item blob as SM_INVENTORY_INFO
    - [✓] `SM_INVENTORY_INFO` + `SM_INVENTORY_ADD_ITEM` — updated: when `item.EnchantLevel > 0`, set `itemMask |= 0x40` and append 1-byte ENCHANT_INFO blob after GENERAL_INFO blob
    - [✓] `SM_INVENTORY_ADD_ITEM` — refactored to call `SM_INVENTORY_INFO.WriteItemInfo` (single blob writer shared across both packets)
    - [✓] `CM_MOVE_ITEM` (0x17E) — fully implemented for all storage combinations: inv↔inv reorder, inv→warehouse, warehouse→inv, warehouse reorder; saves all items (not just changed item) to avoid data loss
    - [✓] `CM_EQUIP_ITEM` — fixed to save `player.Inventory.All` instead of just the changed item
    - [✓] `CM_DIALOG_SELECT` (0x114) — added case WAREHOUSE_OPEN (dialogId=26): sends SM_WAREHOUSE_INFO with current warehouse contents
    - [✓] `CM_MANASTONE` (0x2E8) — implements enchantType=0 (enchantment stone): increments `target.EnchantLevel` up to +15 (no failure), consumes stone (delete or decrement count), saves inventory, sends SM_INVENTORY_ADD_ITEM for updated target; enchantType=1 (manastone socket) is no-op
    - [✓] `ExperienceService.AddGroupExpAsync` — splits XP pool among group members alive in same WorldId; falls back to solo award when not grouped; used by both CM_ATTACK and CM_CASTSPELL
    - [✓] `CM_ATTACK` + `CM_CASTSPELL` — NPC kill XP now calls `AddGroupExpAsync` instead of `AddExpAsync` directly
19. [✓] NPC AI, crafting system, and quest basics (session 2026-04-30) — MIGRATION COMPLETE
    - [✓] `Position.DistanceTo(Position)` — 3D Euclidean distance helper added to Position record struct
    - [✓] `NpcAiService` — BackgroundService with 2-second tick; NPCs with `AggroRange > 0` and `Ai != "dummy"` attack nearest player within range in same WorldId; level×5+rand(5,20) damage; broadcasts SM_ATTACK + SM_ATTACK_STATUS; handles player death (SM_EMOTION(DIE) + SM_DIE)
    - [✓] `Model/Templates/Recipe/RecipeTemplate.cs` + `RecipeComponent.cs` — XmlRoot model for recipe_templates.xml
    - [✓] `DataHolders/RecipeData.cs` — loads recipe_templates.xml; `GetTemplate(id)` lookup
    - [✓] `IDataManager` + `DataManager` — added `RecipeData Recipes` and `QuestData Quests` properties; both loaded in constructor
    - [✓] `CM_CRAFT` (0x12F) — reads unk(C)+targetTemplateId(D)+recipeId(D)+targetObjId(D)+materialsCount(H)+craftType(C); validates components in inventory, consumes them (delete or decrement), creates product item, persists full inventory, sends SM_INVENTORY_ADD_ITEM
    - [✓] `Model/Templates/Quest/QuestTemplate.cs` — XmlRoot with CollectItemsHolder/CollectItem/QuestRewards nested types
    - [✓] `DataHolders/QuestData.cs` — loads quest_data.xml; `GetTemplate(id)` lookup
    - [✓] `Model/Quest/QuestEntry.cs` — QuestStatus enum (START=1/REWARD=2/COMPLETE=3) + QuestEntry with QuestId/Status/Step/CompleteCount
    - [✓] `Model/Quest/PlayerQuestList.cs` — Dictionary-based quest container; Active/Completed computed views
    - [✓] `Player.Quests` (`PlayerQuestList`) — per-player quest state
    - [✓] `V8__quests.sql` — `player_quests` table: player_id/quest_id/status/step/complete_count with composite PK and player index
    - [✓] `IQuestDao` + `QuestDaoImpl` — LoadByPlayerIdAsync, UpsertAsync (INSERT … ON DUPLICATE KEY UPDATE), DeleteAsync
    - [✓] `SM_QUEST_LIST` (0x47) — updated to accept `IEnumerable<QuestEntry>`; writes negated count + per-quest: questId(D)/status(C)/step(D)/completeCount(C) matching Java SM_QUEST_LIST format
    - [✓] `SM_QUEST_COMPLETED_LIST` (0x7B) — updated to accept `IEnumerable<QuestEntry>`; writes negated count + per-quest: questId(D)/completeCount(C)/unk(C)
    - [✓] `CM_ENTER_WORLD` — loads quests from DB into `player.Quests`; passes real quest data to SM_QUEST_LIST + SM_QUEST_COMPLETED_LIST; added `IQuestDao` constructor parameter
    - [✓] `CM_DELETE_QUEST` (0x112) — removes quest from player memory + DB; sends updated SM_QUEST_LIST
    - [✓] `CM_DIALOG_SELECT` (0x114) — added quest accept (dialogId=29/1002): validates template + minLevel, creates QuestEntry(START), persists, sends SM_QUEST_LIST; added quest reward (dialogId=1009): validates collect_items, consumes items, awards exp, marks COMPLETE, sends SM_QUEST_LIST + SM_QUEST_COMPLETED_LIST
    - [✓] `IPlayerDao.UpdateTitleAsync` + `PlayerDaoImpl` — persists `title_id` column update
    - [✓] `CM_TITLE_SET` (0x129) — reads titleId(H); -1/0xFFFF = unequip; updates `player.TitleId`, persists to DB, sends `SM_TITLE_INFO.ActiveTitle`
    - [✓] `CM_USE_ITEM` — fixed single-item save bug: now passes `player.Inventory.All` to `SaveAllAsync` instead of only the modified item
    - [✓] `GsPacketHandlerFactory` — added `IQuestDao` dependency; CM_ENTER_WORLD/CM_DELETE_QUEST/CM_DIALOG_SELECT/CM_CRAFT/CM_TITLE_SET all fully wired
    - [✓] `Program.cs` — registered `IQuestDao → QuestDaoImpl`, `NpcAiService` as BackgroundService
20. [✓] Skill polish, bind point persistence, and level-up broadcast (session 2026-04-30)
    - [✓] `SkillSubType` enum — NONE/ATTACK/HEAL/BUFF/DEBUFF/CHANT/SUMMON/SUMMONHOMING/SUMMONTRAP; added to `Model/Templates/Skill/`
    - [✓] `SkillTemplate` — added `[XmlAttribute("skillsubtype")] SkillSubType SubType` property
    - [✓] `CM_CASTSPELL` (0xE3) — heal detection via `SubType == HEAL`: heal target = self if targetObjectId==0, otherwise look up player by objectId; heal amount = level×6+rand(15,40); broadcasts `SM_ATTACK_STATUS(NaturalHp, LogId.Heal)`; damage check now gates on `!isHealSkill` first to avoid misclassifying heals as damage; buff/chant/unknown → immediate `SM_SKILL_ACTIVATION` broadcast
    - [✓] `SM_RECIPE_LIST` (0xCF) — rewritten to accept `IEnumerable<int>` recipe IDs; wire format: writeH(count) + per-entry: writeD(id)+writeC(0) matching Java source
    - [✓] `RecipeTemplate` — added `[XmlAttribute("race")] Race` property (ELYOS/ASMODIANS)
    - [✓] `RecipeData.GetAutoLearnIds(string race)` — filters by `AutoLearn==1` and race match (or "PC_ALL"); returns IDs as `IEnumerable<int>`
    - [✓] `Player.KnownRecipes` (`HashSet<int>`) — per-player recipe tracking
    - [✓] `CM_ENTER_WORLD` — auto-learns recipes on login via `RecipeData.GetAutoLearnIds(player.Race)`, populates `player.KnownRecipes`, sends real `SM_RECIPE_LIST`
    - [✓] `V9__bind_point.sql` — ALTER TABLE players to add `bind_x`, `bind_y`, `bind_z`, `bind_world_id` (all nullable FLOAT/INT)
    - [✓] `IPlayerDao.UpdateBindPointAsync` — clears or sets bind columns; accepts nullable `Position?`
    - [✓] `PlayerDaoImpl` — `SelectColumns` constant includes bind columns; all Find* queries updated; `PlayerRow` record extended with nullable bind fields; `ToPlayer` maps bind fields to `player.BindPosition`; `UpdateBindPointAsync` implemented
    - [✓] `CM_SHOW_DIALOG` (0x116) — BINDSTONE branch now persists bind point to DB via `UpdateBindPointAsync` in addition to setting in-memory `player.BindPosition`
    - [✓] `ExperienceService` — injected `PlayerConnectionRegistry` and `World`; `AddGroupExpAsync` no longer takes `connRegistry` parameter (uses injected field); `HandleLevelUpAsync` now broadcasts `SM_LEVEL_UPDATE` to all other online players in the same WorldId after sending to the leveling player
    - [✓] `CM_ATTACK` + `CM_CASTSPELL` — updated `AddGroupExpAsync` call sites to drop the now-removed `connRegistry` argument
21. [✓] Quest persistence, revive polish, NPC AI cooldown, NPC regen (session 2026-04-30)
    - [✓] `IQuestDao.SaveAllAsync` + `QuestDaoImpl` — bulk upsert of active quests on disconnect; uses same INSERT…ON DUPLICATE KEY UPDATE as UpsertAsync but reuses a single connection for efficiency
    - [✓] `GsClientConnection.DisposeAsync` — now calls `_questDao.SaveAllAsync(player.Quests.Active)` alongside position/exp/items; quest state can no longer be lost on disconnect
    - [✓] `IQuestDao` injected into `GsClientConnection` + `GsConnectionFactory` — added field, ctor param, and factory wiring
    - [✓] `CM_REVIVE` (0xA7) — when player has no bind point, falls back to `PlayerInitialData.GetSpawnLocation(player.Race)` (Elyos or Asmodian starting zone); converts SpawnLocation → Position and sends SM_TELEPORT_LOC so player always lands somewhere valid
    - [✓] `NpcAiService` — added per-NPC 4-second attack cooldown via `Dictionary<int, DateTime> _lastAttackTime`; NPCs now attack roughly every 4 seconds instead of every 2-second tick
    - [✓] `RegenService` — injected `GameWorld`; added NPC HP regen loop (1% of MaxHp per 6-second tick, silent — no client packet needed)
22. [✓] Startup hardening, display settings persistence, chat config (session 2026-04-30)
    - [✓] `IPlayerDao.ResetAllOnlineAsync()` + `PlayerDaoImpl` — bulk `UPDATE players SET online=0`; called once at startup before spawning to clear stale flags from crashes or unclean shutdowns
    - [✓] `GameServerHost.ExecuteAsync` — injects `IPlayerDao`; calls `ResetAllOnlineAsync` as the first action before script load and NPC spawn
    - [✓] `V10__display_settings.sql` — ALTER TABLE players to add `display_settings SMALLINT DEFAULT 0` and `deny_settings SMALLINT DEFAULT 0`
    - [✓] `IPlayerDao.UpdateDisplaySettingsAsync` + `PlayerDaoImpl` — persists display/deny settings immediately when changed
    - [✓] `PlayerDaoImpl.SelectColumns` — expanded to include `display_settings`, `deny_settings`; `PlayerRow` record and `ToPlayer` updated to map both columns onto `Player.DisplaySettings` / `Player.DenySettings`
    - [✓] `CM_CUSTOM_SETTINGS` (0xAE) — now injects `IPlayerDao` and calls `UpdateDisplaySettingsAsync` before broadcasting; settings survive disconnect
    - [✓] `GsPacketHandlerFactory` — wires `_playerDao` into `CM_CUSTOM_SETTINGS`; adds `CsConnectionOptions` field from `IOptions<CsConnectionOptions>`; passes it to `CM_VERSION_CHECK`
    - [✓] `SM_VERSION_CHECK` — chat server address and port now read from `CsConnectionOptions` (`_cs.Host` parsed via `IPAddress.TryParse`, `_cs.Port`) instead of hardcoded 127.0.0.1:7788; falls back to 127.0.0.1 if host cannot be parsed
    - [✓] `CM_VERSION_CHECK` — injects `CsConnectionOptions` and forwards it to `SM_VERSION_CHECK`
23. [✓] World-entry social broadcast, position guard, combat regen suppression (session 2026-04-30)
    - [✓] `CM_LEVEL_READY` — when players become mutually visible, the newcomer now receives `SM_CUSTOM_SETTINGS` for every already-online player, and every existing player receives `SM_CUSTOM_SETTINGS` for the newcomer; both sides have accurate display/deny state immediately on entry
    - [✓] `CM_ENTER_WORLD` — sends `SM_CUSTOM_SETTINGS` to the entering player during the login sequence (after SM_MOTION) so the client UI reflects the persisted social settings loaded from DB; added `using AionLightning.Game.Model` for `Position`
    - [✓] `CM_ENTER_WORLD` — position guard: if `player.Position.WorldId == 0` (legacy/corrupt row), resets position to race starting zone via `PlayerInitialData.GetSpawnLocation(player.Race)` before spawning
    - [✓] `Player.LastCombatTime` — new `DateTime` property (default `DateTime.MinValue`) tracking when the player last took damage
    - [✓] `NpcAiService` — sets `target.LastCombatTime = now` whenever an NPC successfully damages a player
    - [✓] `RegenService` — added `OutOfCombatDelay = 5s`; skips the entire regen tick for any player whose `LastCombatTime` is within the delay window; HP/MP only regenerates after 5 seconds out of combat
24. [✓] Group HP broadcast, loot cleanup, PvP combat state (session 2026-04-30)
    - [✓] `NpcAiService` — added `BroadcastGroupHpAsync` helper; called after every NPC hit and after player death so group members see real-time HP updates in the group window
    - [✓] `CM_ATTACK` — after dealing damage to a `Player` target: stamps `LastCombatTime` and broadcasts `SM_GROUP_MEMBER_INFO` to the victim's group; extended Task.Run to call `lootSvc.ClearLoot(deadNpc.ObjectId)` after 60s total (3s despawn + 57s hold) to prevent unbounded loot table growth when drops are never collected
    - [✓] `CM_CASTSPELL` heal branch — after healing a `Player` target: broadcasts `SM_GROUP_MEMBER_INFO` to that player's group members so healed HP is reflected in the group window immediately
    - [✓] `CM_CASTSPELL` damage Task.Run — after dealing spell damage to a `Player` target: stamps `LastCombatTime` and broadcasts `SM_GROUP_MEMBER_INFO` to the victim's group; extended loot cleanup to 60s total (matching CM_ATTACK) so spell kills also clean up uncollected drops
25. [✓] Equipment in character list + SM_PLAYER_INFO (session 2026-04-30)
    - [✓] `ItemTemplate` — added `[XmlAttribute("equipment_type")] EquipmentType` property; `IsWeapon` and `IsArmor` computed properties based on it
    - [✓] `SM_CHARACTER_LIST` — constructor now accepts `IReadOnlyList<(Player, PlayerAppearance, IReadOnlyList<Item>)>`; `WritePlayerInfo` accepts optional `IReadOnlyList<Item>?` equipment; writes 13 bytes per visible item (slot 1–4096, armor/weapon only): indicator byte (2 for SUB_HAND/EARRINGS_LEFT/RING_LEFT, else 1) + templateId + godstoneId(0) + color(0); pads to 208; replaced hardcoded display-settings zero with `p.DisplaySettings`
    - [✓] `CM_CHARACTER_LIST` — injected `IItemDao` and `IDataManager`; loads inventory for each character, filters to `IsEquipped && Slot > 0 && Slot <= 4096 && (IsWeapon || IsArmor)`, passes filtered list to `SM_CHARACTER_LIST`
    - [✓] `GsPacketHandlerFactory` — passes `_itemDao` and `_dataManager` to `CM_CHARACTER_LIST`
    - [✓] `SM_PLAYER_INFO` — accepts optional `IEnumerable<Item>? equipment`; builds slot OR-mask and writes it; appends per-item blob (templateId + 0 godstone + 0 dye + enchant glow); `DisplaySettings`/`DenySettings` fields now use real player values
    - [✓] `CM_LEVEL_READY` — passes `player.Inventory.All.Where(i => i.IsEquipped)` to both the self-targeted `SM_PLAYER_INFO` and the cached packet sent to others; existing players now see each other's current equipment on spawn
26. [✓] Creature state, stat init reliability, NpcAiService cleanup, WeaponEquipped toggle (session 2026-04-30)
    - [✓] `SM_PLAYER_INFO` — creature state field changed from hardcoded `0` to `(short)p.State`; dead/resting/flying players now render with the correct visual state on other clients
    - [✓] `NpcAiService` — dead NPCs now remove their entry from `_lastAttackTime` at the start of the tick (instead of only being skipped); prevents unbounded dictionary growth proportional to NPCs killed over server uptime
    - [✓] `CM_EQUIP_ITEM` — after equip/unequip, updates `CreatureState.WeaponEquipped` flag on the player: set when any item occupies slot 1 (MAIN_HAND), cleared otherwise; state is now reflected in the next `SM_PLAYER_INFO` sent to other players
    - [✓] `CM_ENTER_WORLD` — removed brittle `isNewChar = player.MaxHp == 0` detection; MaxHp/MaxMp are now always recomputed from the PlayerStatsTemplate on login (explicit and correct regardless of DB column gaps); starting-item guard simplified to `storedItems.Count == 0`
27. [✓] System messages, group chat, whisper feedback, inventory swap (session 2026-04-30)
    - [✓] `SM_SYSTEM_MESSAGE` (opcode 0x19, NEW) — minimal implementation: writes colorId(0x19)+dialect(0x00)+npcObjId(0)+msgCode(D)+paramCount(C)+string params+shoutFlag(0x00); factory method `NoSuchUser(name)` uses msg code 1300627 matching Java STR_NO_SUCH_USER
    - [✓] `CM_CHAT_MESSAGE_WHISPER` — when target player is offline (`GetByName` returns null), sends `SM_SYSTEM_MESSAGE.NoSuchUser` back to the sender instead of silently dropping the message
    - [✓] `CM_CHAT_MESSAGE_PUBLIC` — group chat (channel 0x05) now delivered only to party members via `player.Group.Members`; replaced the no-op stub; legion chat (0x0A) remains a no-op pending legion system
    - [✓] `CM_MOVE_ITEM` inv→inv reorder — when target bag slot is already occupied, the displaced item is swapped to the dragged item's old slot; both items sent in `SM_INVENTORY_ADD_ITEM` response; eliminates duplicate-slot corruption
    - [✓] `CM_MOVE_ITEM` warehouse→warehouse reorder — same swap logic applied; prevents slot conflicts in personal warehouse
28. [✓] Revive stand broadcast + attacker combat stamp (session 2026-04-30)
    - [✓] `CM_REVIVE` — added `SM_EMOTION(STAND)` broadcast immediately after `SM_EMOTION(RESURRECT)`; without STAND, client death animations on other players' screens were never cleared, leaving the corpse pose visible after resurrection
    - [✓] `CM_ATTACK` — stamps `player.LastCombatTime = DateTime.UtcNow` after dealing damage to any target (NPC or player); previously only the victim's combat timer was updated, allowing the attacker to regenerate HP/MP freely during melee combat
    - [✓] `CM_CASTSPELL` damage Task.Run — same fix: stamps `player.LastCombatTime` after computing spell damage so the caster is in combat and cannot regen between casts
29. [✓] Data-driven NPC drop tables (session 2026-04-30)
    - [✓] `DropData` (`DataHolders/DropData.cs`) — loads all `npc_drops/*.xml` files; `NpcDropGroup` + `NpcDropEntry` model (itemId/chance/minAmount/maxAmount); `GetDropGroups(npcId)` and `HasDrops(npcId)` queries; chance stored as 0–100 float, clamped on load
    - [✓] `IDataManager` + `DataManager` — added `DropData Drops` property; loaded last after `Quests`; all `npc_drops/*.xml` files are already copied to output via the `AL-Game/data/static_data/**` glob in the csproj
    - [✓] `LootService` — injected `IDataManager`; `GenerateDrops(npc)` now looks up `Drops.GetDropGroups(npc.Template.NpcId)`; for each drop group, each entry is rolled independently (d100 < chance); random count sampled in [minAmount, maxAmount]; kinah always drops; falls back to 50% Minor Life Elixir only when no data table exists for the NPC; constructor signature change is resolved automatically by the DI container
30. [✓] NPC target lock + leash range (session 2026-04-30)
    - [✓] `Npc` model — added `HomePosition` property (the spawn point set at creation); semantically separates "where the NPC lives" from "where it currently is" for future pathfinding
    - [✓] `SpawnService.SpawnNpc` — sets `HomePosition = position` alongside `Position`; respawn path also inherits this via same call
    - [✓] `NpcAiService` — added `_npcTargets: Dictionary<int npcObjectId, int playerObjectId>` for target lock; on each tick: locked target is validated (alive, same world, within leash range = 1.5× AggroRange from HomePosition) — cleared on failure; only when no locked target does the NPC scan for a new nearest player in AggroRange; target is cleared immediately after a kill so the NPC idles rather than staying stuck in combat state; `_npcTargets` cleared for all NPCs when no players are online; leash and scan distances both measured from `HomePosition` rather than current position
31. [✓] NPC combat state + regen suppression (session 2026-04-30)
    - [✓] `Creature.LastCombatTime` — moved from `Player` to `Creature` base class so both players and NPCs track combat state; default `DateTime.MinValue` (never in combat) unchanged
    - [✓] `Player.cs` — removed duplicate `LastCombatTime` property (now inherited from `Creature`)
    - [✓] `CM_ATTACK` — now stamps `target.LastCombatTime = combatNow` unconditionally before the `if (target is Player)` branch; covers both Player-vs-Player and Player-vs-NPC damage; removed the now-redundant per-branch `damagedPlayer.LastCombatTime` assignment
    - [✓] `CM_CASTSPELL` damage Task.Run — same change: single `target.LastCombatTime = combatNow` assignment before the `if (target is Player)` branch; NPC targets now correctly enter combat when spell-damaged
    - [✓] `NpcAiService` — stamps `npc.LastCombatTime = now` alongside `target.LastCombatTime` when the NPC deals damage; NPC is now in combat on both sides of each exchange
    - [✓] `RegenService` NPC loop — added `if (now - npc.LastCombatTime < OutOfCombatDelay) continue;` guard matching the player regen check; NPCs can no longer regen HP while actively being attacked or within 5 seconds of combat
32. [✓] Zone-aware broadcasts (WorldId filtering) (session 2026-04-30)
    - [✓] `CM_LEVEL_READY` — player-to-player introduction loop now filters `other.Position.WorldId != worldId`; NPC introduction now filters to `npc.Position.WorldId == worldId`; entering players only receive entities in their own zone, not all entities across the entire server
    - [✓] `SpawnService.ScheduleRespawn` — respawn broadcast checks `conn.ActivePlayer?.Position.WorldId == position.WorldId`; players in other zones no longer receive SM_NPC_INFO for NPCs that respawned in a different map
    - [✓] `CM_ATTACK` NPC death Task.Run — `SM_DELETE` broadcast checks `c.ActivePlayer?.Position.WorldId == npcWorldId`; players in other zones no longer receive the despawn packet for NPCs they can't see
    - [✓] `CM_CASTSPELL` NPC death path — same WorldId filter applied to both `SM_EMOTION(DIE)` and `SM_DELETE` broadcasts; matches the CM_ATTACK pattern
33. [✓] Zone-aware movement and combat broadcasts (session 2026-04-30)
    - [✓] `CM_MOVE` — movement broadcast (`SM_MOVE`) now only goes to players in the same WorldId; fixes cross-zone position spam
    - [✓] `CM_EMOTION` — emotion/state broadcast (`SM_EMOTION`) now only goes to players in the same WorldId
    - [✓] `CM_ATTACK.BroadcastAsync` — all attack-related broadcasts (SM_ATTACK, SM_ATTACK_STATUS, SM_EMOTION(DIE)) now filtered to attacker's WorldId
    - [✓] `CM_CASTSPELL.BroadcastAsync` — all spell-related broadcasts (SM_CASTSPELL, SM_SKILL_ACTIVATION, SM_ATTACK_STATUS, etc.) now filtered to caster's WorldId
    - [✓] `NpcAiService` — SM_ATTACK + SM_ATTACK_STATUS broadcast filtered to NPC's WorldId (`npc.HomePosition.WorldId`); SM_EMOTION(DIE) on player kill also filtered; this corrects the remaining cross-zone NPC combat visibility gap
34. [✓] Remaining zone-unfiltered broadcasts — full audit pass (session 2026-04-30)
    - [✓] `CM_EQUIP_ITEM` — `SM_UPDATE_PLAYER_APPEARANCE` broadcast now checks `other.ActivePlayer?.Position.WorldId == worldId`; players in other zones no longer receive equipment appearance updates for players they can't see
    - [✓] `CM_REVIVE` — both `SM_EMOTION(RESURRECT)` and `SM_EMOTION(STAND)` broadcasts now filtered to `player.Position.WorldId`; revive animation only visible to players in the same zone
    - [✓] `CM_TARGET_SELECT` — `SM_TARGET_UPDATE` broadcast now filtered to caster's WorldId; cross-zone players no longer receive target-state packets for players in other zones
    - [✓] `CM_CASTSPELL` heal path — `SM_ATTACK_STATUS(Heal)` broadcast was using `_connRegistry.GetAll()` without a zone guard (bypassed `BroadcastAsync`); now checks `c.ActivePlayer?.Position.WorldId == healWorldId` before each send
    - [✓] `CM_CASTSPELL` damage Task.Run — `SM_SKILL_ACTIVATION` and `SM_ATTACK_STATUS(SpellAtk)` were also using `registry.GetAll()` without a zone guard; both now filtered to `castWorldId` (captured before the cast delay to avoid stale position reads)
    - [✓] `CM_CASTSPELL` player-death path inside Task.Run — `SM_EMOTION(DIE)` broadcast for a player killed by a spell now filtered to `castWorldId`; matches the NPC-death path that was fixed in Item 33
35. [✓] Player disconnect visibility + air movement broadcast (session 2026-04-30)
    - [✓] `GsClientConnection.DisposeAsync` — on player disconnect, captures `worldId` before unregistering, then broadcasts `SM_DELETE(player.ObjectId)` to all remaining connections in the same zone; previously disconnecting players left ghost entities visible to others until they moved or relogged
    - [✓] `CM_MOVE_IN_AIR` (0xF3) — injected `PlayerConnectionRegistry`; after updating player position from fly-teleport data, now broadcasts `SM_MOVE` to zone peers (same WorldId filter as CM_MOVE); fly paths were previously invisible to all other players
    - [✓] `GsPacketHandlerFactory` — updated 0xF3 registration to pass `_connRegistry` to `CM_MOVE_IN_AIR`
36. [✓] Macro persistence (session 2026-04-30)
    - [✓] `player_macrosses` table already exists in DB schema (`player_id`, `order` INT, `macro` TEXT, UNIQUE KEY on `(player_id, order)`)
    - [✓] `IMacroDao` + `MacroDaoImpl` — `LoadByPlayerIdAsync`, `UpsertAsync` (INSERT … ON DUPLICATE KEY UPDATE), `DeleteAsync`; `order` is a MySQL reserved word — all queries use backtick-quoted `` `order` ``
    - [✓] `SM_MACRO_RESULT` (0xE8) — new packet; single byte: 0 = Created, 1 = Deleted; two static singletons
    - [✓] `SM_MACRO_LIST` (0xE7) — updated constructor to accept `IEnumerable<KeyValuePair<int,string>>` macros; writes negated count (`-count`) per Aion protocol convention, then per-entry: position (byte) + XML (UTF-16LE string)
    - [✓] `Player.Macros` — added `Dictionary<int, string>` keyed by position (1-48)
    - [✓] `CM_MACRO_CREATE` — reads `position` (byte) + `xml` (string); upserts to `player.Macros` + DB; replies with `SM_MACRO_RESULT.Created`
    - [✓] `CM_MACRO_DELETE` — reads `position` (byte); removes from `player.Macros` + DB; replies with `SM_MACRO_RESULT.Deleted`
    - [✓] `CM_ENTER_WORLD` — injected `IMacroDao`; loads macros from DB into `player.Macros` after quests; sends two SM_MACRO_LIST packets split at position 24 (Java two-part convention)
    - [✓] `GsPacketHandlerFactory` — added `IMacroDao` field; wired into CM_MACRO_CREATE (0x14D), CM_MACRO_DELETE (0x172), CM_ENTER_WORLD (0xAA)
    - [✓] `Program.cs` — registered `IMacroDao → MacroDaoImpl`
37. [✓] Chat zone-scoping + warehouse on login (session 2026-04-30)
    - [✓] `CM_CHAT_MESSAGE_PUBLIC` — Normal (0x00) and Shout (0x03) now broadcast only to players in the same WorldId; previously both went to all players server-wide regardless of zone
    - [✓] `CM_CHAT_MESSAGE_PUBLIC` — unknown channel bytes (0x01, 0x02, etc.) now silently dropped; previously they fell through to `SM_MESSAGE.ChatType.Normal` and broadcast to all players (wrong format + wrong scope); confirmed via Java ChatType enum: 0x01/0x02 are not valid Aion 4.6.0 channel types
    - [✓] `CM_ENTER_WORLD` — added `await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct)` after SM_INVENTORY_INFO sends; warehouse items are now sent to the client on login so the warehouse UI is pre-populated without requiring an NPC visit
38. [✓] DB migration gaps + packet safety + zone-enter motion (session 2026-04-30)
    - [✓] `Sql/game/V7__player_bind_display_macros.sql` — adds missing `bind_x/bind_y/bind_z/bind_world_id` (nullable float/int) and `display_settings/deny_settings` (smallint, default 0) columns to `players`; creates `player_macrosses` table matching Java schema; without this migration the `PlayerDaoImpl` SELECT would fail at runtime with a missing-column error
    - [✓] `GsClientConnection.OnPacketAsync` — wrapped `packet.Read` + `packet.RunAsync` in try-catch; `OperationCanceledException` re-thrown, all other exceptions logged at Error level and swallowed so the connection stays alive; previously any unhandled exception in a packet handler (e.g. NullReferenceException, PacketFormatException) would silently kill the read loop and disconnect the player
    - [✓] `CM_LEVEL_READY` — added `await _conn.SendAsync(SM_MOTION.Broadcast(other.ObjectId), ct)` in the per-existing-player loop; entering players now receive motion state for all already-online players in the zone so they see correct combat stances rather than default standing pose

39. [✓] Player inspection + PlayerDaoImpl INSERT fix (session 2026-04-30)
    - [✓] `PlayerDaoImpl.InsertAsync` — INSERT now explicitly includes `display_settings, deny_settings` columns with values `0, 0`; previously newly created players would get DB NULL for those columns, causing a read-back failure after V7 migration added `smallint NOT NULL DEFAULT 0`
    - [✓] `SM_VIEW_PLAYER_DETAILS` (0x41) — new server packet; sends another player's equipped items to the inspecting client; uniqueId zeroed for privacy; GENERAL_INFO blob format matches SM_WAREHOUSE_INFO; opcode confirmed from Java `ServerPacketsOpcodes.java`
    - [✓] `CM_VIEW_PLAYER_DETAILS` (0x106) — rewritten from stub; reads `targetObjectId` (int); looks up live target via `World.GetPlayerByObjectId`; sends `SM_VIEW_PLAYER_DETAILS` to the requesting connection; null-safe if target left zone between click and packet arrival
    - [✓] `GsPacketHandlerFactory` — updated 0x106 from `new CM_VIEW_PLAYER_DETAILS()` to `new CM_VIEW_PLAYER_DETAILS(conn, _world)`

40. [✓] Missing SQL migrations + CM_OPEN_STATICDOOR zone filter (session 2026-04-30)
    - [✓] `V8__enchant_warehouse.sql` — adds `storage_type` (tinyint, default 0) and `enchant_level` (tinyint, default 0) to `player_items`; adds composite index `player_storage(player_id, storage_type)`; these columns were already used by `ItemDaoImpl` since Item 18 but no migration created them — any item load/save would fail at runtime without this migration
    - [✓] `V9__quests.sql` — creates `player_quests` table (`player_id`/`quest_id`/`status`/`step`/`complete_count`, composite PK, FK to players with CASCADE); table was used by `QuestDaoImpl` since Item 19 but never materialised in any migration file — quest load/save/delete would all throw table-not-found at runtime without this
    - [✓] `CM_OPEN_STATICDOOR` (0xF5) — door-open `SM_EMOTION(OPEN_DOOR)` broadcast was missing WorldId filter; added `int worldId = player.Position.WorldId` guard so only players in the same zone see the door animation; matches the zone-filtering pattern applied to all other broadcasts in Items 32–34

41. [✓] Loot wire format + inventory save correctness (session 2026-04-30)
    - [✓] `SM_LOOT_ITEMLIST` — `nonTradeable` byte was inverted: we wrote `1` for tradeable items but the Aion 4.6.0 protocol writes `1` for NON-tradeable items; fixed to `item.IsTradeable ? 0 : 1`; confirmed against Java SM_LOOT_ITEMLIST.java writeImpl
    - [✓] `LootService.GenerateDrops` — kinah drop was marked `IsTradeable: false` which would cause it to show as non-tradeable in loot window after the above fix; kinah is always tradeable in Aion so changed to `IsTradeable: true`
    - [✓] `CM_LOOT_ITEM` — `SaveAllAsync` was called with a single-item array `[kinahItem]` / `[existing]` / `[item]` which causes `ReplaceItemsAsync` to DELETE all inventory items and re-insert only that one item; data loss only visible on server crash (clean disconnect re-saves everything from memory); refactored to update the item in memory first, then call `SaveAllAsync(player.Inventory.All)` once; also removed duplicate `SM_INVENTORY_ADD_ITEM` sends (was sent in both branches, now sent once after both branches)

42. [✓] SaveAllAsync subset bug — full audit and fix (session 2026-04-30)
    - Root cause: `ItemDaoImpl.ReplaceItemsAsync` does a DELETE-all + INSERT for the given list; any call that passes a subset (e.g., `[kinahItem]`) silently wipes all other inventory items from the DB; data is safe until the next subset save or a crash, but the window is real
    - [✓] `CM_BUY_ITEM.BuyFromShopAsync` — removed per-field `SaveAllAsync([kinahItem])` and `SaveAllAsync(itemsAdded)` calls; now does all in-memory mutations first, then one `SaveAllAsync(player.Inventory.All)` before notifying the client
    - [✓] `CM_BUY_ITEM.SellToShopAsync` — same fix: removed `SaveAllAsync(itemsToUpdate)` and `SaveAllAsync([kinahItem])`; single `SaveAllAsync(player.Inventory.All)` at the end covers all deletions, partial-count updates, and kinah gain; targeted `DeleteAsync` calls removed as redundant (SaveAllAsync does the full replace)
    - [✓] `CM_SPLIT_ITEM` — both branches passed `[source, target]` / `[source, newItem]`; changed to `player.Inventory.All` in both paths
    - [✓] `CM_TELEPORT_SELECT` — kinah deduction called `SaveAllAsync([kinah])`; changed to `player.Inventory.All`
    - All other callers confirmed correct (`CM_EQUIP_ITEM`, `CM_CRAFT`, `CM_MANASTONE`, `CM_DIALOG_SELECT`, `CM_SEND_MAIL`, `CM_GET_MAIL_ATTACHMENT`, `CM_GM_COMMAND_SEND`, `CM_EXCHANGE_LOCK`, `CM_MOVE_ITEM`, `CM_LOOT_ITEM` already fixed in Item 41)

49. [✓] Friend online/offline presence notifications (session 2026-04-30)
    - [✓] `CM_ENTER_WORLD` — after sending SM_FRIEND_LIST to the logging-in player, iterates their friends; for each friend that is currently online, loads that friend's own friend list and sends them an updated SM_FRIEND_LIST with the newly correct online IDs; previously a friend's list showed stale offline status for the logging-in player until they manually reopened the social window
    - [✓] `GsClientConnection.DisposeAsync` — after unregistering the disconnecting player from `_connRegistry` (so their ObjectId is already absent from online IDs), loads their friend list and sends each online friend an updated SM_FRIEND_LIST showing the disconnecting player now offline; previously friends never saw the player go offline
    - [✓] `GsClientConnection` — injected `ISocialDao`; added to constructor and field
    - [✓] `GsConnectionFactory` — injected `ISocialDao`; added to constructor and passed through to `GsClientConnection.Create`
    - Build: 0 warnings, 0 errors

59. [✓] CM_CUSTOM_SETTINGS zone filter; NpcAiService duplicate HP broadcast (session 2026-04-30)
    - [✓] `CM_CUSTOM_SETTINGS` — added `Position.WorldId` zone filter to broadcast loop; previously sent display/deny settings update to ALL online players regardless of zone; Java `broadcastPacket(player, packet, true)` is zone-scoped to KnownList (neighbors only); players in other zones neither care about nor need the setting update
    - [✓] `NpcAiService.TickAsync` — removed duplicate `BroadcastGroupHpAsync(target, ct)` call at death check: was called once at `if (target.CurrentHp > 0) continue` (correct) and redundantly again at the death handling block; group members received two identical HP=0 updates per kill tick
    - Build: 0 warnings, 0 errors

58. [✓] CM_REVIVE revive message + group HP notification; CM_MOVE_ITEM UniqueId cast (session 2026-04-30)
    - [✓] `SM_SYSTEM_MESSAGE` — added `Revived()` factory (code 1300738 = STR_REBIRTH_MASSAGE_ME); mirrors Java `PlayerReviveService` which sends this on every bind/kisk revive
    - [✓] `CM_REVIVE.RunAsync` — after stats update, now sends `SM_SYSTEM_MESSAGE.Revived()` to self; also broadcasts `SM_GROUP_MEMBER_INFO(Update)` to group members so their HP bars reflect 25% after resurrection; previously group members saw HP bar frozen at 0 after revive
    - [✓] `CM_MOVE_ITEM` — removed erroneous `(int)` cast in `new SM_DELETE_ITEM((int)item.UniqueId)`: `SM_DELETE_ITEM` already accepts `long`, so the narrowing cast was redundant and could corrupt uniqueIds > int.MaxValue
    - Build: 0 warnings, 0 errors

57. [✓] SM_GROUP_INFO and SM_GROUP_MEMBER_INFO wire format corrected to match Java protocol (session 2026-04-30)
    - [✓] `SM_GROUP_INFO` — rewrote wire format: now sends group metadata only (groupId, leaderId, leaderWorldId, loot rules × 6 DWORDs, constant 2, TeamType 1, subType 0, empty string), matching Java SM_GROUP_INFO.writeImpl(); previously wrote all member data inline which the client could not parse
    - [✓] `SM_GROUP_MEMBER_INFO` — full rewrite: added `GroupEvent` enum (Leave=0x00, Movement=0x01, Disconnected=0x03, Join=0x05, EnterOffline=0x07, Update=0x09, Enter=0x13); HP/MP/FP changed from WriteH shorts to WriteD ints; added flight points (written as 0), unk DWORD, duplicated mapId, class id, gender id, event byte, channel short, mentor byte; event-specific tail (LEAVE: H+C; JOIN/EnterOffline: name; Enter/Update: name+D); matches Java writeImpl() exactly
    - [✓] `CM_INVITE_TO_GROUP` — replaced single SM_GROUP_INFO-to-all broadcast with correct sequence: SM_GROUP_INFO to joining player → SM_GROUP_MEMBER_INFO(Join) to self → SM_GROUP_MEMBER_INFO(Enter) to/from each existing member; matches Java PlayerEnteredEvent
    - [✓] `GsClientConnection.DisposeAsync` — replaced SM_GROUP_INFO dissolution broadcast with SM_GROUP_MEMBER_INFO(Disconnected) to remaining members; matches Java PlayerDisconnectedEvent
    - [✓] `RegenService`, `NpcAiService`, `CM_ATTACK`, `CM_CASTSPELL` — all stat-update calls updated to SM_GROUP_MEMBER_INFO(groupId, member, GroupEvent.Update)
    - Build: 0 warnings, 0 errors

56. [✓] SM_INVENTORY_INFO equip filter, CM_BUY_ITEM kinah error, FindByItemId equip guard, quest selectable reward items (session 2026-04-30)
    - [✓] `CM_ENTER_WORLD` — SM_INVENTORY_INFO call now filters to `!i.IsEquipped` items only; equipped items (bitmask Slot values 1, 2, 4 etc.) were included in the bag packet and appeared as duplicate items in wrong bag slots; equipped items are already sent via SM_UPDATE_PLAYER_APPEARANCE / SM_PLAYER_INFO
    - [✓] `CM_BUY_ITEM.BuyFromShopAsync` — added SM_SYSTEM_MESSAGE.NoEnoughKinah() send on insufficient kinah before the early return; previously the purchase silently failed with no client feedback; `SM_SYSTEM_MESSAGE` now has `NoEnoughKinah()` factory (code 1300137)
    - [✓] `PlayerInventory.FindByItemId` — added `bool includeEquipped = false` parameter; default excludes equipped items from bag searches; quest collect items and craft components could previously consume currently-equipped items if they shared the same ItemId; kinah lookups in CM_BUY_ITEM/SellToShopAsync pass `includeEquipped: false` which is correct (kinah is never equipped)
    - [✓] `QuestTemplate.QuestRewards` — added `List<SelectableRewardItem> SelectableItems` (XmlElement "selectable_reward_item"); new `SelectableRewardItem` class with `ItemId` + `Count` XmlAttributes; quest_data.xml has `<selectable_reward_item>` in most quests but the model had no field for it
    - [✓] `CM_DIALOG_SELECT` — stored `extendedRewardIndex` (previously discarded ReadH) in `_rewardIndex`; `HandleQuestRewardAsync` now grants the selected item at `_rewardIndex` from `template.Rewards.SelectableItems`; stacks on existing inventory item if found, creates new item otherwise; persists full inventory and sends SM_INVENTORY_ADD_ITEM
    - Build: 0 warnings, 0 errors

55. [✓] Item.Slot semantics fix, CM_DELETE_ITEM equip guard, CM_CRAFT partial notification, CM_SPLIT_ITEM full-stack split (session 2026-04-30)
    - [✓] `Item.cs` — replaced computed `IsEquipped => Slot >= 0 && StorageType == 0` with an explicit `bool IsEquipped { get; set; } = false`; the computed form was ambiguous because `Slot` holds the equipment bitmask for equipped items AND the bag position for bag items (both non-negative), causing any dragged bag item to appear permanently equipped — blocking trade, mail, deletion, and further dragging
    - [✓] `Sql/game/V10__item_is_equipped.sql` — adds `is_equipped TINYINT(1) NOT NULL DEFAULT 0` to `player_items`; back-fills `is_equipped = 1` for rows where `slot > 0 AND storage_type = 0` (the existing convention that equipped items had slot = equipment bitmask)
    - [✓] `ItemDaoImpl` — SELECT now reads `is_equipped` (column 6); INSERT now writes `@IsEquipped`; `LoadItemsAsync` sets `IsEquipped = reader.GetBoolean(6)` on the returned `Item`
    - [✓] `CM_EQUIP_ITEM` — now explicitly sets `item.IsEquipped = true` on equip and `item.IsEquipped = false` (+ `displaced.IsEquipped = false`) on unequip; previously relied on Slot value alone; displaced-item search now filters on `i.IsEquipped` to avoid false positives from bag items at the same slot number
    - [✓] `CM_DELETE_ITEM` — added `|| item.IsEquipped` to entry guard so equipped gear cannot be deleted; previously allowed deletion of any item including currently worn weapons/armor
    - [✓] `CM_CRAFT` — consumption loop now tracks items whose Count > 0 after subtraction in `partiallyConsumed`; sends `SM_INVENTORY_ADD_ITEM(partiallyConsumed)` before the product packet so the client UI shows the correct remaining ingredient count; previously only fully-consumed items sent SM_DELETE_ITEM, leaving partial stacks showing stale counts
    - [✓] `CM_SPLIT_ITEM` — guard changed from `source.Count <= _splitAmount` to `source.Count < _splitAmount` so a full-stack "move" (split all) is accepted; zero-count source is now removed from inventory + DB + client (SM_DELETE_ITEM) matching Java's `Storage.split` behavior; previously a full-stack split was silently rejected with no feedback
    - Build: 0 warnings, 0 errors

54. [✓] Regen broadcast, GM heal broadcast, GM cross-zone tp appearance (session 2026-04-30)
    - [✓] `RegenService.TickAsync` — HP and MP regen packets now broadcast to all players in the same WorldId, not just the regenerating player's own connection; previously zone peers saw each other's HP/MP bars frozen after out-of-combat until the player next attacked; mirrors Java's `broadcastPacketAndReceive(owner, SM_ATTACK_STATUS)` pattern; broadcast wrapped in `try/catch` per the pattern in NpcAiService to keep the tick robust if a peer connection is mid-disconnect
    - [✓] `CM_GM_COMMAND_SEND.HandleHeal` — HP and MP SM_ATTACK_STATUS packets now broadcast to zone peers after the self-send; previously only the GM saw their own heal animation; mirrors the same zone broadcast used by CM_CASTSPELL heal-skill path
    - [✓] `CM_GM_COMMAND_SEND.HandleTeleport` — after cross-zone GM `.tp`, now broadcasts `SM_PLAYER_INFO` (with equipped items) to all peers already in the destination zone; previously the GM appeared invisible to destination-zone players and had to disconnect/reconnect to become visible; uses the same equipment + appearance construction as CM_TELEPORT_SELECT.cs
    - Build: 0 warnings, 0 errors

53. [✓] Dead-player consumable exploit + SM_LOOT_STATUS enum completeness (session 2026-04-30)
    - [✓] `CM_USE_ITEM` — added `|| player.IsAlreadyDead` to the entry guard; a dead player (CurrentHp == 0 / State has CreatureState.Dead) could previously use an HP/MP elixir and restore health without going through any resurrection flow, leaving them alive server-side but visually dead to all other clients; fix mirrors the same guard pattern at CM_ATTACK:46 and CM_CASTSPELL:72
    - [✓] `SM_LOOT_STATUS` — extended `State` enum from `{ Open=0, Close=1 }` to `{ Open=0, Close=1, Locked=2, Empty=3 }` matching Java DropService; states 2 and 3 have no callers yet but are required for the future DropService migration (freeForAll timer, closeDropList broadcast); adding them now prevents a compile gap when that module is ported
    - Audit also confirmed: SM_LOOT_STATUS Open is NOT broadcast on NPC death (matches Java retail behavior — the loot-icon is client-driven from SM_EMOTION DIE); NpcAiService counter-attack cooldown is already correctly implemented; player death path SM_DIE/CreatureState.Dead is correct in CM_ATTACK, CM_CASTSPELL, and NpcAiService
    - Build: 0 warnings, 0 errors

52. [✓] Air-movement flood, cross-zone revive spawn, loot-close broadcast, respawn home position (session 2026-04-30)
    - [✓] `CM_MOVE_IN_AIR` — removed the zone-peer broadcast loop entirely; Java reference (CM_MOVE_IN_AIR.java) does not broadcast air-movement position fixes — observing clients interpolate from the original fly-teleport destination; the .NET version was re-broadcasting every rapid in-flight update packet causing O(N²) traffic identical to the CM_MOVE bug fixed in Item 51; also removed unused `_connRegistry` dependency and simplified `RunAsync` to synchronous return
    - [✓] `GsPacketHandlerFactory` — CM_MOVE_IN_AIR call simplified to `new CM_MOVE_IN_AIR(conn)` (no longer passes `_connRegistry`)
    - [✓] `CM_REVIVE` — cross-zone bind-revive now follows the same pattern as CM_TELEPORT_SELECT (Item 51): skip emotion broadcast to new-zone peers (they have no entity for this player yet); instead fire-and-forget `SchedulePostReviveSpawnAsync` which after 2200ms sends SM_CHANNEL_INFO + SM_PLAYER_SPAWN to self; CM_LEVEL_READY from the client will then introduce the player to new-zone peers via SM_PLAYER_INFO; same-zone revive keeps existing RESURRECT/STAND emotion broadcast intact
    - [✓] `CM_LOOT_ITEM` — when loot bag empties, SM_LOOT_STATUS Close is now broadcast to all players in the same WorldId (not just the looter); injected `PlayerConnectionRegistry`; previously party members saw a stale loot icon on an already-emptied corpse
    - [✓] `GsPacketHandlerFactory` — CM_LOOT_ITEM now receives `_connRegistry`
    - [✓] `SpawnService.ScheduleRespawn` — changed `npc.Position` to `npc.HomePosition` so NPCs always respawn at their original spawn-table coordinates, not wherever they died; dormant today (AI never moves NPCs) but would cause respawn-location drift once NPC movement is added in a later milestone
    - Build: 0 warnings, 0 errors

51. [✓] Movement broadcast flood, teleport SM_DELETE jump animation, post-teleport spawn sequence (session 2026-04-30)
    - [✓] `CM_MOVE` — broadcast loop now gated on movement-state transitions only: `if ((_type & MovementMask.StartMove) == 0 && _type != 0) return;`; mid-movement heartbeat packets (type != 0 and no STARTMOVE flag) are no longer relayed to zone peers — they only update server-side state; observing clients interpolate the trajectory locally from the start packet; previously every CM_MOVE was re-broadcast to all zone peers creating O(N²) packet traffic
    - [✓] `CM_TELEPORT_SELECT` — removed `oldWorldId != loc.MapId` guard on SM_DELETE broadcast; now always sends `SM_DELETE(objectId, time: 11)` (jump-out animation) to old-zone peers on every teleport; previously same-map teleports sent no departure notification so peers saw the player vanish silently without the jump animation
    - [✓] `CM_TELEPORT_SELECT.SchedulePostTeleportAsync` — new private method fires-and-forgets a 2200ms delayed follow-up matching Java TeleportService2.changePosition: cross-map sends SM_CHANNEL_INFO + SM_PLAYER_SPAWN (client then sends CM_LEVEL_READY which broadcasts player to new-zone peers); same-map sends SM_PLAYER_INFO + SM_STATS_INFO + SM_MOTION to self plus SM_PLAYER_INFO to zone peers at new position; previously the client was left at a loading/black screen after cross-map teleport because SM_PLAYER_SPAWN was never sent
    - Build: 0 warnings, 0 errors

50. [✓] Trade execution item-duplication guard + mail equipped-item bypass (session 2026-04-30)
    - [✓] `CM_EXCHANGE_LOCK.ExecuteTradeAsync` — added pre-transfer validation loop: before touching either inventory, verifies every initiator item is still present in `ini.Inventory` and every target item in `tgt.Inventory`; if any are missing the trade is cancelled via `CancelAsync`; previously `Remove()` was called without checking its return value, so an item missing due to a concurrent operation (e.g. another GM command) would be duplicated in the recipient's inventory
    - [✓] `CM_SEND_MAIL` — item attachment guard changed from `if (item is not null)` to `if (item is not null && !item.IsEquipped)`; previously a player could remove an item from an equip slot by attaching it to a mail without unequipping it first, breaking the equip slot state; now matches the same check used in CM_EXCHANGE_ADD_ITEM
    - Build: 0 warnings, 0 errors

48. [✓] Login sequence gaps — SM_BIND_POINT_INFO, SM_FRIEND_LIST, SM_BLOCK_LIST; character-select unread mail (session 2026-04-30)
    - [✓] `CM_ENTER_WORLD` — injected `ISocialDao`; after SM_GAME_TIME now sends `SM_BIND_POINT_INFO` with the player's stored bind position (or race starting-zone coords if not yet bound); without this the client had no idea where the player was bound and the UI revive-to-bind-point button pointed nowhere
    - [✓] `CM_ENTER_WORLD` — added `SM_FRIEND_LIST` and `SM_BLOCK_LIST` at end of login sequence; friends loaded via `ISocialDao.GetFriendsAsync`, blocks via `GetBlocksAsync`; online status computed from `_connRegistry.GetAll()`; previously the social lists were never sent so the client showed empty friend/block lists until the player opened the social window manually
    - [✓] `IMailDao` — added `HasUnreadAsync(playerId)` method; implemented in `MailDaoImpl` with a targeted `COUNT(*)` query instead of loading all mails
    - [✓] `CM_CHARACTER_LIST` — injected `IMailDao`; per-character `HasUnreadAsync` result is passed through to `SM_CHARACTER_LIST` and written as `1`/`0` in the unread-mail field; previously the field was always `0` so the mail icon never appeared in character select
    - [✓] `SM_CHARACTER_LIST` — signature updated to accept `bool HasUnread` per character tuple; no wire-format change beyond the corrected field value
    - [✓] `GsPacketHandlerFactory` — passes `_socialDao` to CM_ENTER_WORLD and `_mailDao` to CM_CHARACTER_LIST
    - Build: 0 warnings, 0 errors

47. [✓] Teleporter NPC dialog — SM_TELEPORT_MAP + AIRLINE_SERVICE handler (session 2026-04-30)
    - [✓] `TeleportData` — added `_npcTeleportIds: Dictionary<int, int>`; `LoadNpcTeleporters` now parses `teleportId` attribute from each `<teleporter_template>` element and stores it per NPC id; new `GetTeleportId(npcId) → int?` method exposed; previously the teleportId was silently discarded and there was no way to retrieve it
    - [✓] `SM_TELEPORT_MAP` (0xC4) — new server packet; wire format `D(objectId) + H(teleportId)` matches Java SM_TELEPORT_MAP; client uses teleportId to look up available destinations from its own client-side data and render the teleporter map UI
    - [✓] `CM_DIALOG_SELECT` — added `AIRLINE_SERVICE = 44` constant; new case: looks up NPC by objectId, retrieves teleportId via `_dataManager.Teleports.GetTeleportId(npc.Template.NpcId)`, sends `SM_TELEPORT_MAP`; previously clicking "Travel" on a teleporter NPC sent nothing and the teleporter map UI never appeared
    - Build: 0 warnings, 0 errors

46. [✓] Max-level cap bug, SM_STATS_INFO after item use, .level/.tp GM command persistence + zone-exit, newcomer SM_ABNORMAL_EFFECT (session 2026-04-30)
    - [✓] `PlayerExperienceTable.GetLevelForExp` — off-by-one fixed: `level >= MaxLevel ? MaxLevel - 1 : level` → `level > MaxLevel ? MaxLevel : level`; a max-level player (e.g. level 65) was incorrectly capped at level 64 because `>=` fired on equality; now returns the correct cap only when the loop somehow overshoots MaxLevel (impossible in practice but safe to guard)
    - [✓] `CM_USE_ITEM` — `SM_STATS_INFO(player)` (1-arg, uses stale cached stats) replaced with `SM_STATS_INFO(player, statTpl, _dataManager.ExpTable)`; HP/MP bars now reflect current level template after consuming a potion instead of showing incorrect values
    - [✓] `CM_GM_COMMAND_SEND` — injected `IPlayerDao`; `.level` command now calls `UpdateExpLevelAsync` after updating in-memory `player.Exp` and `player.Level`; previously the level set by a GM was lost on next login
    - [✓] `CM_GM_COMMAND_SEND.HandleTeleport` — added cross-zone `SM_DELETE(player.ObjectId)` broadcast to old-zone players before updating `player.Position`; GM `.tp` across zones no longer leaves a ghost player visible in the source zone
    - [✓] `CM_LEVEL_READY` — newcomer intro loop now sends `SM_ABNORMAL_EFFECT(other.ObjectId, isPlayer: true)` for each existing player to the entering player, matching what the entering player's own `SM_ABNORMAL_EFFECT` does in the other direction; without it existing players appeared with no status-effect data on the newcomer's client
    - [✓] `GsPacketHandlerFactory` — passes `_playerDao` to `CM_GM_COMMAND_SEND`
    - Build: 0 warnings, 0 errors

45. [✓] Quest delete protocol, equip-slot collision, item-consume UI, zone-exit visibility (session 2026-04-30)
    - [✓] `CM_DELETE_QUEST` — now sends `SM_QUEST_ACTION(questId)` (action=3/Delete) before `SM_QUEST_LIST`; previously only SM_QUEST_LIST was sent, so the client tracker UI never removed the quest entry
    - [✓] `CM_EQUIP_ITEM` — equip action (action=0) now finds any item already occupying the target slot and moves it to the bag (slot=-1) before assigning the new item; previously two items could share the same slot value, causing the client to show both as equipped; displaced item included in `SM_INVENTORY_ADD_ITEM` alongside the newly equipped item
    - [✓] `CM_DIALOG_SELECT.HandleQuestRewardAsync` — collect-item consumption loop now tracks partially-consumed items (count > 0 after subtraction); sends `SM_INVENTORY_ADD_ITEM(partiallyConsumed)` after `SaveAllAsync`; previously the client showed stale counts for partially consumed stacks
    - [✓] `CM_TELEPORT_SELECT` — injected `PlayerConnectionRegistry`; before changing `player.Position`, if destination WorldId differs from current WorldId, broadcasts `SM_DELETE(player.ObjectId)` to all players in the old zone; players in the old zone now correctly see the teleporting player despawn
    - [✓] `CM_REVIVE` — same cross-zone SM_DELETE broadcast added; refactored to compute `destination` position first, then check old vs. new WorldId before updating `player.Position`; revive to bind point / starting zone now removes the player from the old zone's visibility list
    - [✓] `GsPacketHandlerFactory` — passes `_connRegistry` to CM_TELEPORT_SELECT
    - Build: 0 warnings, 0 errors

44. [✓] Quest kill tracking + SM_QUEST_ACTION + CM_SPLIT_ITEM stack overflow fix (session 2026-04-30)
    - [✓] `QuestTemplate` — added `[XmlElement("quest_kill")] List<QuestKill> QuestKills`; new `QuestKill` class parses `npc_ids` (space-separated NPC IDs) and `seq` (slot index) attributes; lazy-parses `NpcIds` into `HashSet<int>` on first access; `count` attribute (default 1) is the required kill count per slot
    - [✓] `QuestEntry` — replaced flat `Step` int with packed QuestVars encoding matching Java: `_vars[6]` array where each slot holds 0-63; `GetVar(idx)` / `SetVar(idx, val)` accessors; `Step` property packs/unpacks via `vars[i] * 64^i` — stored in `step` DB column; `CompleteCount` moved to separate property (no longer conflicts with vars); no DB schema change needed
    - [✓] `SM_QUEST_ACTION` (0x7C) — new server packet; `ActionType` enum: Accept=1 (quest accept), StepUpdate=2 (kill progress / completion), Delete=3; wire format matches Java `writeImpl`: `C(action) + D(questId)` then per-action body
    - [✓] `QuestService` — new singleton service; `HandleNpcKillAsync(player, deadNpc, conn, ct)`: iterates player's active quests, checks each `QuestKill.NpcIds.Contains(deadNpc.NpcId)`, increments `vars[kill.Seq]` up to `kill.Count`, persists via `IQuestDao.UpsertAsync`, sends `SM_QUEST_ACTION(StepUpdate)` to the killing player
    - [✓] `CM_ATTACK` — injected `QuestService`; calls `HandleNpcKillAsync` after NPC death confirmed and drops generated
    - [✓] `CM_CASTSPELL` — injected `QuestService`; captures `questSvc` + `conn` before Task.Run; calls `HandleNpcKillAsync` in NPC death path inside Task.Run with `CancellationToken.None`
    - [✓] `CM_DIALOG_SELECT.HandleQuestAcceptAsync` — now sends `SM_QUEST_ACTION(Accept)` before `SM_QUEST_LIST` so the client quest tracker UI updates immediately on accept
    - [✓] `CM_DIALOG_SELECT.HandleQuestRewardAsync` — added kill-requirement validation before consuming collect items: iterates `template.QuestKills`, checks `entry.GetVar(kill.Seq) >= kill.Count`; returns early if any slot is incomplete; sends `SM_QUEST_ACTION(StepUpdate, COMPLETE)` before `SM_QUEST_LIST` on successful reward
    - [✓] `CM_SPLIT_ITEM` — injected `IDataManager`; merge branch now checks `target.Count + splitAmount > template.MaxStackCount` before modifying counts; rejects silently if merge would exceed cap
    - [✓] `GsPacketHandlerFactory` — added `QuestService` field + ctor param; wired into CM_ATTACK, CM_CASTSPELL, CM_SPLIT_ITEM
    - [✓] `Program.cs` — registered `QuestService` as singleton
    - Build: 0 warnings, 0 errors

43. [✓] Exchange kinah UniqueId=0 bug fix (session 2026-04-30)
    - Root cause: `CM_EXCHANGE_LOCK.AddOrStackKinah` was a synchronous static helper that called `new Item { ... }` without generating a UniqueId when the receiving player had no kinah in their inventory; `UniqueId = 0` is invalid and would collide with any other zero-id row in the DB
    - [✓] Removed `AddOrStackKinah` static method; inlined async kinah creation in `ExecuteTradeAsync` using `await _itemDao.NextUniqueIdAsync(ct)` for the create-new-item branch
    - Mutual-kinah-exchange edge case: when both players trade kinah, both are validated non-null before transfer so no new-item path is hit; UniqueId fix only triggers when exactly one side has no kinah yet
    - [✓] Verified `CM_EXCHANGE_ADD_KINAH` already reads the trailing unknown dword (`r.ReadD(); // unk`) — wire format was already correct, no change needed
    - Build: 0 warnings, 0 errors

65. [✓] Broadcast robustness sweep 2 — remaining unprotected zone sends (session 2026-04-30)
    - [✓] `CM_LEVEL_READY` — all 8 SendAsync calls in the zone-peer introduction loop now wrapped in try/catch; a disconnecting peer no longer aborts SM_PLAYER_INFO / SM_MOTION / SM_ABNORMAL_EFFECT / SM_CUSTOM_SETTINGS delivery to remaining connections; same fix applied to NPC introduction loop
    - [✓] `CM_MOVE` — SM_MOVE peer broadcast loop wrapped in try/catch
    - [✓] `CM_EMOTION` — self-send and peer-send both wrapped in try/catch (consistent with other broadcast handlers)
    - [✓] `SpawnService.ScheduleRespawn` — SM_NPC_INFO respawn broadcast loop wrapped in try/catch
    - [✓] `RegenService.BroadcastGroupMemberUpdateAsync` — SM_GROUP_MEMBER_INFO group loop wrapped in try/catch
    - Build: 0 warnings, 0 errors

64. [✓] Broadcast robustness — try/catch in all zone broadcast loops (session 2026-04-30)
    - [✓] `NpcAiService.TickAsync` — added `_lastAttackTime.Clear()` alongside existing `_npcTargets.Clear()` when player count drops to zero; prevents stale timestamps delaying first attack when players re-enter a zone
    - [✓] `CM_CHAT_MESSAGE_PUBLIC` — Normal/Shout zone broadcast loop now wraps each `SendAsync` in `try { } catch { }` to match the group-chat branch; a disconnecting peer can no longer abort the broadcast for subsequent players
    - [✓] `CM_ATTACK.BroadcastAsync` — wrapped both self-send and peer-sends in try/catch; a failing connection no longer cuts off the broadcast for remaining zone peers
    - [✓] `CM_CASTSPELL.BroadcastAsync` — same fix applied
    - [✓] `CM_CASTSPELL` Task.Run body — added try/catch to every `SendAsync` call inside the fire-and-forget damage/death/despawn path: `SM_SKILL_ACTIVATION`, `SM_ATTACK_STATUS`, `SM_EMOTION(DIE)`, `SM_DIE`, `SM_DELETE` broadcasts
    - [✓] `CM_CASTSPELL` heal broadcast — `SM_ATTACK_STATUS(Heal)` zone loop now wrapped in try/catch
    - [✓] `CM_ATTACK` Task.Run despawn loop — `SM_DELETE` broadcast wrapped in try/catch
    - Build: 0 warnings, 0 errors

63. [✓] NPC per-template speed + CM_RECIPE_DELETE (session 2026-04-30)
    - [✓] `NpcStatsTemplate` — added `[XmlElement("speeds")] CreatureSpeeds? Speeds` and `RunSpeed` helper; NPC XML `<speeds>` element now parsed correctly
    - [✓] `Creature.MovementSpeed` — changed from expression-body constant `=> 6.0f` to `{ get; protected set; } = 6.0f` so subclasses can set it
    - [✓] `Npc` constructor — sets `MovementSpeed = template.Stats?.RunSpeed ?? 6.0f`; aggressive NPCs (e.g. run_fight=8.0) now report their template run speed in SM_NPC_INFO instead of a hardcoded 6.0f
    - [✓] `CM_RECIPE_DELETE` (0x13B) — was a no-op stub; now injects `GsClientConnection`, reads recipeId (D), removes from `player.KnownRecipes`, sends refreshed `SM_RECIPE_LIST`; wired in `GsPacketHandlerFactory`
    - Build: 0 warnings, 0 errors

62. [✓] SM_NPC_INFO real data + NpcTemplate enrichment (session 2026-04-30)
    - [✓] `NpcTemplate` — added `[XmlAttribute("title_id")] TitleId`, `[XmlAttribute("adelay")] AttackDelay` (default 1500), `[XmlElement("bound_radius")] NpcBoundRadius?` with Front/Side/Upper floats
    - [✓] `SM_NPC_INFO` — `state` now uses `(short)_npc.State` (actual creature state: 65=normal, 33=fight, 7=dead) instead of hardcoded 65; `titleId` from `tpl.TitleId`; `boundRadius front` from `tpl.BoundRadius?.Front ?? tpl.Height`; `movementSpeed` from `_npc.MovementSpeed` (6.0f); `attackDelay` from `tpl.AttackDelay` (real XML value, falls back to 1500); `targetObjectId` from `_npc.Target?.ObjectId ?? 0` so clients see NPC combat targeting state
    - Build: 0 warnings, 0 errors

61. [✓] GsClientConnection group-disband survivor notification on disconnect (session 2026-04-30)
    - [✓] `GsClientConnection.DisposeAsync` — after sending SM_GROUP_MEMBER_INFO(Disconnected) to remaining members, now checks if the group fell below 2 members; sends SM_LEAVE_GROUP_MEMBER + SM_GROUP_INFO(dissolution) to the sole survivor to clear their group UI; previously the survivor's party window remained visible (showing the disconnected player as absent) until they relogged
    - Build: 0 warnings, 0 errors

70. [✓] SM_SKILL_LIST notification + skill book chat message (session 2026-04-30)
    - [✓] `SM_SKILL_LIST` — added optional `msgId`, `skillName`, `skillLevel` constructor parameters; when `msgId != 0`, writes `skillName (S) + skillLevel (H)` after the msgId field; matches Java SM_SKILL_LIST.writeImpl() which shows "[SkillName] Lv.X has been learned" in system chat
    - [✓] `SkillTreeData` — added `GetSkillName(skillId)` method: returns the Name from the first tree entry for a given skillId; used by CM_USE_ITEM to populate the chat notification
    - [✓] `CM_USE_ITEM.HandleSkillBookAsync` — now passes `msgId: 1300050, skillName, skillLevel` to SM_SKILL_LIST; players see a system chat notification when learning a skill from a book
    - Build: 0 warnings, 0 errors

69. [✓] Skill book item support (session 2026-04-30)
    - [✓] `ItemTemplate` — added `SkillLearnAction` class parsing `<skilllearn skillid="N" class="..." level="N"/>` from item_templates.xml; `ItemActions` gains `[XmlElement("skilllearn")] SkillLearnAction? SkillLearn`; `ItemTemplate` exposes `SkillLearnId` computed property
    - [✓] `SkillTreeData` — added `_bySkillId` secondary index (populated in `Load` alongside existing hash index); added `GetMaxSkillLevel(skillId, cls, race, playerLevel)` method that finds the highest skill level available from the tree for a given skill at the player's current progression — returns 1 when no tree entry exists
    - [✓] `CM_USE_ITEM` — added `HandleSkillBookAsync` branch checked before the existing elixir path: validates class restriction (string comparison against PlayerClass.ToString()), validates RequiredLevel, checks `player.Skills.IsPresent(skillId)` to prevent re-learning, calls `SkillTreeData.GetMaxSkillLevel` for skill level, calls `player.Skills.AddSkill`, sends `SM_SKILL_LIST([entry], isNew: true)`, deletes item from inventory + DB + client (SM_DELETE_ITEM); skill books are always consumed (non-stackable by design)
    - Build: 0 warnings, 0 errors

86. [✓] Legion title display + legion chat (session 2026-04-30)
    - [✓] `SM_LEGION_UPDATE_TITLE` (0x72) — D(objectId)+D(legionId)+S(name)+C(rankDisplay); rankDisplay: BG→0, Deputy/Centurion→1, else→2; shows legion name above player head in client
    - [✓] `CM_ENTER_WORLD` — after SM_LEGION_MEMBERLIST, sends SM_LEGION_UPDATE_TITLE to self and broadcasts it to all online members already in the zone
    - [✓] `CM_LEVEL_READY` — during zone-peer introduction, sends SM_LEGION_UPDATE_TITLE for each player (self to others, others to self) when either has a legion
    - [✓] `CM_CHAT_MESSAGE_PUBLIC` — channel 0x0A (Legion): delivers to all online members of `player.Legion`; previously was silently dropped as unknown channel
    - Build: 0 warnings, 0 errors

85. [✓] Legion login integration + SM_LEGION_UPDATE_MEMBER (session 2026-04-30)
    - [✓] `SM_LEGION_UPDATE_MEMBER` (0x71) — D(objId)+C(rank)+C(class)+C(level)+D(worldId)+C(isOnline)+D(lastOnline)+D+D+S; used for online/offline status notifications
    - [✓] `CM_ENTER_WORLD` — injected ILegionDao + LegionService; after macros, calls GetMemberLegionAsync; if player is in a legion, loads full Legion from DB into service (or reuses cached instance if another member is already online); sets player.Legion; after friend-list sends, sends SM_LEGION_INFO + SM_LEGION_MEMBERLIST to self, then SM_LEGION_UPDATE_MEMBER(online) to all other online members
    - [✓] `GsPacketHandlerFactory` — CM_ENTER_WORLD now receives _legionDao + _legionService
    - Build: 0 warnings, 0 errors

84. [✓] Legion system — create/invite/leave/kick/rank/announcement (session 2026-04-30)
    - [✓] `V11__legions.sql` — `legions` (id/name/level/contribution_points/announcement/permissions) + `legion_members` (player_id/legion_id/rank_id/self_intro/nickname) with FK constraints
    - [✓] `LegionRank` enum (BrigadeGeneral=0/Deputy=1/Centurion=2/Legionary=3/Volunteer=4)
    - [✓] `LegionMember` class — objectId/name/rank/classId/level/worldId/isOnline/selfIntro/nickname
    - [✓] `Legion` class — id/name/level/rank/points/announcement/permissions + `Dictionary<int,LegionMember>` members
    - [✓] `ILegionDao` + `LegionDaoImpl` — CRUD via MySqlConnector + Dapper; `GetMemberLegionAsync` JOINs players; `AddMemberAsync` is idempotent via ON DUPLICATE KEY UPDATE
    - [✓] `LegionService` — `ConcurrentDictionary<legionId,Legion>` + `ConcurrentDictionary<playerId,Legion>`; AddLegion/AddMember/RemoveMember/RemoveLegion; GetByPlayerId/GetById/GetByName
    - [✓] `Player.Legion` (`Legion?`) — nullable property; type aliased as `LegionModel` in Player.cs to avoid CS0118 with namespace
    - [✓] `SM_LEGION_INFO` (0x6E) — S(name)+C(level)+D(rank)+H×4(perms)+Q(points)+D×3+announcement(S+D)+B(26)
    - [✓] `SM_LEGION_ADD_MEMBER` (0x6F) — D(objId)+S(name)+C(rank)+C(isMember)+C(class)+C(level)+D(worldId)+D+D+S
    - [✓] `SM_LEGION_LEAVE_MEMBER` (0x70) — D(objId)+C(0)+D(0)+D(0)+S(name)+S(name)
    - [✓] `SM_LEGION_MEMBERLIST` (0x9D) — C(isFirst)+H(count)+per-member: D+S+C(class)+D(level)+C(rank)+D(world)+C(online)+S+S+D+D+D+C+C+C+C
    - [✓] `SM_LEGION_EDIT` (0x9E) — type byte; 0x05 announcement: S+D(time); 0x06 disband: D(time)
    - [✓] `CM_LEGION` fully implemented: 0x00 create (validate name, DB insert, BG member, notify); 0x01 invite (BG-only, add member, broadcast SM_LEGION_ADD_MEMBER); 0x02 leave (remove self, broadcast SM_LEGION_LEAVE_MEMBER, disband if last); 0x04 kick (BG-only); 0x05/06/07 rank changes; 0x09 announcement (DB update, SM_LEGION_EDIT broadcast)
    - [✓] `GsClientConnection.DisposeAsync` — on disconnect, marks member offline in-memory, sends SM_LEGION_LEAVE_MEMBER to remaining members; does NOT disband on disconnect
    - [✓] `GsConnectionFactory` + `GsPacketHandlerFactory` + `Program.cs` wired with LegionService + ILegionDao
    - Build: 0 warnings, 0 errors

83. [✓] Legion emblem + summon-move read-format fixes — 8 stub packets (session 2026-04-30)
    - [✓] `CM_LEGION_WH_KINAH` (0x2EE) — Q(amount)+C(operation)
    - [✓] `CM_LEGION_UPLOAD_EMBLEM` (0x163) — D(size)+B(size) (variable-length emblem image)
    - [✓] `CM_LEGION_UPLOAD_INFO` (0x162) — D+C+C+C+C (legionId, RGBA)
    - [✓] `CM_LEGION_SEND_EMBLEM_INFO` (0xD2) — D(legionId)
    - [✓] `CM_LEGION_SEND_EMBLEM` (0xCD) — D(legionId)
    - [✓] `CM_LEGION_MODIFY_EMBLEM` (0x119) — D+C+C+C+C+C+C (legionId, RGBA, type, id)
    - [✓] `CM_LEGION_TABS` (0x115) — D(legionId)+C(tabId)
    - [✓] `CM_SUMMON_MOVE` (0x16B) — D+F×3+C+C(type) then: if StartMove+Mouse→F×3; if Glide→C; if Vehicle→D+D+F×3; vector absent for summons when Mouse not set (Java comment)
    - Remaining 21 stubs confirmed empty in Java — intentionally no-op Read()
    - Build: 0 warnings, 0 errors

82. [✓] Store/legion/pet/misc read-format fixes — 11 stub packets (session 2026-04-30)
    - [✓] `CM_APPEARANCE` (0x2E6) — C(type)+C+H+D then if type 0/1: S
    - [✓] `CM_CHARACTER_EDIT` (0x13E) — D(objectId)+B(52)
    - [✓] `CM_IN_GAME_SHOP_INFO` (0x183) — C+D+D+S+S
    - [✓] `CM_ITEM_REMODEL` (0x138) — D(npcId)+D(keepItemId)+D(extractItemId)
    - [✓] `CM_MEGAPHONE` (0x1B4) — S(chatMessage)+D(itemObjectId)
    - [✓] `CM_PETITION` (0xF6) — H(action) then if 2→D; else→S
    - [✓] `CM_REGISTER_HOUSE` (0x1B2) — Q(bidKinah)+Q(unk1)
    - [✓] `CM_PRIVATE_STORE` (0x155) — H(itemCount)+N×(D+D+H+D)
    - [✓] `CM_LEGION` (0xCF) — C(exOpcode) then per-case: create/invite/kick/appoint/demote/announce/intro→D+S; leave/refresh/levelup→D+H; permissions→H×4; nickname→S+S
    - [✓] `CM_PET` (0xF4) — H(actionId) then: ADOPT(1)→D+D+C+D+D+D+D+S; SURRENDER/SPAWN/DISMISS(2/3/4)→D; FOOD(9)→D+if3:D else:D+D+D; RENAME(10)→D+S; MOOD(12)→D+D
    - [✓] `CM_PET_EMOTE` (0xF7) — C(emoteId) then: MOVE_STOP(0)→F×3+C; MOVETO(12)→F×3+C+F×3; else→C+C
    - Build: 0 warnings, 0 errors

81. [✓] Housing + summon + misc read-format fixes — 18 stub packets (session 2026-04-30)
    - [✓] `CM_CHARGE_ITEM` (0x2EC) — D+C+H(count)+N×D
    - [✓] `CM_FAST_TRACK_CHECK` (0x1B9) — D(accountId)
    - [✓] `CM_GROUP_DATA_EXCHANGE` (0x2ED) — C(action) then if 1→D; else C+C+D
    - [✓] `CM_HOUSE_DECORATE` (0x2E9) — D+D+H
    - [✓] `CM_HOUSE_EDIT` (0x110) — C(actionId) then action-specific D or D+F+F+F+C+H+H+H
    - [✓] `CM_HOUSE_KICK` (0x2EA) — C+H
    - [✓] `CM_HOUSE_OPEN_DOOR` (0x1A0) — D(address)+C(leaveFlag)
    - [✓] `CM_HOUSE_PAY_RENT` (0x1BD) — C(weekCount)
    - [✓] `CM_HOUSE_SCRIPT` (0xFC) — D+C+H(total)+if>0: D(compressed)+if<8150: D(uncompressed)+B(compressed)
    - [✓] `CM_HOUSE_SETTINGS` (0x2EB) — C+C+S
    - [✓] `CM_HOUSE_TELEPORT` (0x1BC) — C+D+D
    - [✓] `CM_REPORT_PLAYER` (0x19D) — B(1)+S(playerName)
    - [✓] `CM_SUMMON_CASTSPELL` (0x16F) — D+H+C+D+F
    - [✓] `CM_SUMMON_COMMAND` (0x15B) — C+D+D+D
    - [✓] `CM_SUMMON_EMOTION` (0x168) — D+C
    - [✓] `CM_USE_HOUSE_OBJECT` (0x1A2) — D(itemObjectId)
    - [✓] `CM_REPLACE_ITEM` (0x170) — C+D+C+D (readSC → ReadC, same 1-byte width)
    - [✓] `CM_UNK` (0x10F) — D+C+C+C+C+D+D+D+D+D+D matching Java readImpl()
    - Build: 0 warnings, 0 errors

80. [✓] Broker + place-bid read-format fixes — 9 stub packets (session 2026-04-30)
    - [✓] `CM_BROKER_LIST` (0x159) — populated Read(): `D(brokerId)+C(sortType)+H(page)+H(listMask)`
    - [✓] `CM_BROKER_SEARCH` (0x15E) — populated Read(): `D+C+H+H+H(count)+N×D`
    - [✓] `CM_BROKER_REGISTERED` (0x15F) — populated Read(): `D(npcId)`
    - [✓] `CM_REGISTER_BROKER_ITEM` (0x15D) — populated Read(): `D(brokerId)+D(itemId)+Q(price)+H(count)`
    - [✓] `CM_BUY_BROKER_ITEM` (0x15C) — populated Read(): `D(brokerId)+D(itemId)+H(count)`
    - [✓] `CM_BROKER_CANCEL_REGISTERED` (0x142) — populated Read(): `D(npcId)+D(brokerItemId)`
    - [✓] `CM_BROKER_SETTLE_ACCOUNT` (0x140) — populated Read(): `D(npcId)`
    - [✓] `CM_BROKER_SETTLE_LIST` (0x143) — populated Read(): `D(npcId)`
    - [✓] `CM_PLACE_BID` (0x1BF) — populated Read(): `D(listIndex)+Q(bidOffer)`
    - Build: 0 warnings, 0 errors

79. [✓] Batch read-format fixes — 6 stub packets (session 2026-04-30)
    - [✓] `CM_PLAYER_LISTENER` (0xCA) — fixed Read(): removed extra `r.ReadC()` (Java reads nothing; over-consuming 1 byte corrupted next packet)
    - [✓] `CM_GM_BOOKMARK` (0x11E) — populated Read(): `r.ReadS()` (command+playerName string)
    - [✓] `CM_CHALLENGE_LIST` (0x18A) — populated Read(): `C+D+C+D+D` (action+taskOwner+ownerType+playerId+dateSince)
    - [✓] `CM_CAPTCHA` (0xAC) — populated Read(): `C(type)` then if type 0x00/0x01: `C(count)+S(word)`
    - [✓] `CM_CHARACTER_PASSKEY` (0x190) — populated Read(): `H(type)+B(32)` then if type 2: `B(32)` (passkey as UTF-16LE fixed 32-byte blocks)
    - [✓] `CM_FIND_GROUP` (0x2EF) — populated Read(): `C(action)` then case 0x01: `D+D`; case 0x02: `D+H+H+D+S`
    - Build: 0 warnings, 0 errors

78. [✓] Batch read-format fixes — 10 stub packets (session 2026-04-30)
    - [✓] `CM_READ_EXPRESS_MAIL` (0x160) — fixed Read(): `r.ReadD()` → `r.ReadC()` (Java reads 1-byte action, not 4-byte int)
    - [✓] `CM_RELEASE_OBJECT` (0x1A3) — populated Read(): `r.ReadD()` (targetObjectId)
    - [✓] `CM_SELECTITEM_OK` (0x18E) — populated Read(): `r.ReadD()+r.ReadD()+r.ReadC()` (uniqueItemId+unk+index)
    - [✓] `CM_QUESTIONNAIRE` (0x153) — populated Read(): `r.ReadD()+r.ReadH()+N×r.ReadD()` (objectId+count+itemIds)
    - [✓] `CM_DISTRIBUTION_SETTINGS` (0x19B) — populated Read(): `r.ReadD()+r.ReadD()` (unk1+lootRule)
    - [✓] `CM_COMPOSITE_STONES` (0x192) — populated Read(): `r.ReadD()+r.ReadD()+r.ReadD()` (tool+item1+item2)
    - [✓] `CM_BREAK_WEAPONS` (0x16D) — populated Read(): `r.ReadD()+r.ReadD()` (unk+weaponId)
    - [✓] `CM_FUSION_WEAPONS` (0x16C) — populated Read(): `r.ReadD()+r.ReadD()+r.ReadD()` (unk+item1+item2)
    - [✓] `CM_GROUP_LOOT` (0x19A) — populated Read(): 4×D+4×C+D+C+D matching Java 4.6 readImpl()
    - [✓] `CM_INSTANCE_INFO` (0x182) — populated Read(): `r.ReadD()+r.ReadC()` (unk1+unk2)
    - Build: 0 warnings, 0 errors

77. [✓] SM_MARK_FRIENDLIST + CM_MARK_FRIENDLIST impl + 9 read-format fixes (session 2026-04-30)
    - [✓] `SM_MARK_FRIENDLIST` (0x117) — new server packet: `WriteD(playerObjectId); WriteC(1); WriteH(0);` signals friend list delivery complete
    - [✓] `CM_MARK_FRIENDLIST` (0x10C) — implemented: reads nothing; queries `ISocialDao.GetFriendsAsync`, builds online-ids set, sends `SM_FRIEND_LIST` then `SM_MARK_FRIENDLIST`; wired in factory with conn+socialDao+connRegistry
    - [✓] `CM_BONUS_TITLE` (0x18B) — populated Read(): `r.ReadH()` (bonusTitleId; 0xFFFF=clear); stub impl
    - [✓] `CM_CHAT_GROUP_INFO` (0x11F) — populated Read(): `r.ReadS() + r.ReadD()`; stub impl
    - [✓] `CM_CHECK_MAIL_SIZE` (0x127) — populated Read(): `r.ReadC()` (mailSize); stub impl
    - [✓] `CM_AUTO_GROUP` (0x16A) — populated Read(): `r.ReadD() + r.ReadC() + r.ReadC()`; stub impl
    - [✓] `CM_OBJECT_SEARCH` (0xA9) — populated Read(): `r.ReadD()` (npcId); stub impl
    - [✓] `CM_WINDSTREAM` (0x2E4) — populated Read(): `r.ReadD() + r.ReadD() + r.ReadD()`; stub impl
    - [✓] `CM_SHOW_BRAND` (0x197) — populated Read(): `r.ReadD() + r.ReadD() + r.ReadD()`; stub impl
    - [✓] `CM_ABYSS_RANKING_PLAYERS` (0x19E) — populated Read(): `r.ReadC()` (raceId); stub impl
    - [✓] `CM_ABYSS_RANKING_LEGIONS` (0x154) — populated Read(): `r.ReadC()` (raceId); stub impl
    - Build: 0 warnings, 0 errors

76. [✓] Batch read-format fixes — 5 stub packets (session 2026-04-30)
    - [✓] `CM_REMOVE_ALTERED_STATE` (0xE1) — fixed Read(): was `r.ReadD()` → `r.ReadH()` (Java reads H/short for skillId, not D/int; over-consuming 2 bytes corrupted next packet)
    - [✓] `CM_SUMMON_ATTACK` (0x169) — populated empty Read(): `r.ReadD(summonObjectId); r.ReadD(targetObjectId); r.ReadC(unk); r.ReadH(time); r.ReadC(unk);` matching Java readImpl(); RunAsync stub remains
    - [✓] `CM_GODSTONE_SOCKET` (0x139) — populated empty Read(): `r.ReadD(npcObjectId); r.ReadD(weaponId); r.ReadD(stoneId);` matching Java readImpl(); RunAsync stub remains
    - [✓] `CM_BUY_TRADE_IN_TRADE` (0x13A) — populated empty Read(): `r.ReadD(sellerObjectId); r.ReadD(itemId); r.ReadD(count);` matching Java readImpl(); RunAsync stub remains
    - [✓] `CM_FAST_TRACK` (0x13E) — populated empty Read(): `r.ReadH(action); r.ReadH(unk); r.ReadD(unk); r.ReadD(unk); r.ReadD(unk); r.ReadD(unk);` matching Java readImpl(); RunAsync stub remains
    - Build: 0 warnings, 0 errors

75. [✓] CM_PLAY_MOVIE_END read-format fix + CM_CHAT_PLAYER_INFO offline check (session 2026-04-30)
    - [✓] `CM_PLAY_MOVIE_END` (0x113) — fixed Read() from D(movieId) to C(type)+D(targetObjectId)+D(dialogId)+H(movieId)+D(unk) matching Java readImpl(); incorrect format was consuming 2 wrong bytes from subsequent packets; RunAsync remains a no-op (quest engine movie-end triggers not yet implemented)
    - [✓] `SM_SYSTEM_MESSAGE` — added `PlayerOffline()` factory (code 1300046 = STR_MSG_ASK_PCINFO_LOGOFF); used when a player requests info for an offline player
    - [✓] `CM_CHAT_PLAYER_INFO` (0xC5) — reads playerName(S); if target is offline (not found in connRegistry), sends `SM_SYSTEM_MESSAGE.PlayerOffline()`; online case is a no-op (SM_CHAT_WINDOW not yet implemented — client handles absence gracefully); wired in factory with conn + connRegistry
    - Build: 0 warnings, 0 errors

74. [✓] Duel system — auto-accept PvP duels (session 2026-04-30)
    - [✓] `DuelService` — `ConcurrentDictionary<int,int>` keyed by participantObjectId → opponentObjectId; `IsDueling`, `GetOpponent`, `StartDuel`, `EndDuel`, `RemovePlayer` methods
    - [✓] `SM_DUEL` (opcode 0xB9) — type 0x00 (STARTED): C(0)+D(opponentObjId); type 0x01 (RESULT): C(1)+C(resultId)+D(msgId)+S(name); factory methods `Started(opponentId)`, `Won(opponentName)` (resultId=2, msg=1300098), `Lost(opponentName)` (resultId=0, msg=1300099)
    - [✓] `CM_DUEL_REQUEST` (0x130) — reads targetObjectId(D); validates target exists, alive, not self, neither player already dueling; calls `DuelService.StartDuel`; sends `SM_DUEL.Started` to both players (auto-accept — no SM_QUESTION_WINDOW); wired with conn+connRegistry+duelService+world
    - [✓] `CM_ATTACK` — added `DuelService` dependency; player-death path checks `GetOpponent(player.ObjectId) == deadPlayer.ObjectId`; on duel win: sets `deadPlayer.CurrentHp=1`, calls `EndDuel`, sends `SM_DUEL.Won` to winner and `SM_DUEL.Lost` to loser, returns without applying CreatureState.Dead or SM_DIE
    - [✓] `CM_CASTSPELL` Task.Run — same duel-win check added to player-death path; captured `duelSvc` before Task.Run to avoid closure over `this`
    - [✓] `GsClientConnection` — added `DuelService` dependency; calls `_duelService.RemovePlayer(player.ObjectId)` in DisposeAsync before LeaveGroup so disconnect clears any active duel
    - [✓] `GsConnectionFactory` — added `DuelService` ctor param; forwards to `GsClientConnection.Create`
    - [✓] `GsPacketHandlerFactory` — added `DuelService` ctor param; wired into CM_DUEL_REQUEST, CM_ATTACK, CM_CASTSPELL
    - [✓] `Program.cs` — registered `DuelService` as singleton
    - Build: 0 warnings, 0 errors

79. [✓] Trade/LFG/Region channels + GroupLeader chat (session 2026-05-01)
    - [✓] `SM_MESSAGE.ChatType` — added `GroupLeader=0x07`, `Trade=0x0E`, `Lfg=0x0F`, `Region=0x10`; all four use the same wire format as Group/Legion (senderName+message), confirmed against Java SM_MESSAGE.writeImpl() which handles CH1-CH3 in the same switch branch
    - [✓] `CM_CHAT_MESSAGE_PUBLIC` — added handling for: 0x07 (GroupLeader) → routes to group members with ChatType.GroupLeader; 0x0E/0x0F/0x10 (Trade/LFG/Region) → server-wide broadcast to all online players; previously all four channel bytes were silently dropped as "unknown"; GroupLeader shares the group membership check with 0x05 group chat
    - Build: 0 warnings, 0 errors

78. [✓] NpcAiService — npc.Target tracking + return-home state machine (session 2026-05-01)
    - [✓] `NpcAiService` — added `ReturnState` record and `_returnState` dictionary; `StopChaseAsync` no longer snaps `npc.Position` to HomePosition immediately — instead it sets `_returnState[npc.ObjectId]` with the travel arrival time and sends the return-move packet; `TickAsync` checks each NPC's `_returnState` before any aggro/wander logic: if arrived → snap position + remove state; if not arrived → skip AI for this tick so NPCs can't be re-aggro'd mid-return
    - [✓] `NpcAiService` — `npc.Target` now set to the locked `Player` when a combat target is acquired; cleared to `null` when the target is lost (leash break), when the player is killed, and when the NPC dies; fixes `SM_NPC_INFO` `targetObjectId` which previously was always 0 for zone-entering players during active NPC combat
    - [✓] `NpcAiService` — `_returnState` cleared in the dead-NPC cleanup block and in the no-players-online early-exit; prevents stale return entries accumulating for dead or despawned NPCs
    - Build: 0 warnings, 0 errors

77. [✓] Enchant glow broadcast, recipe book animation, loot distance guard (session 2026-05-01)
    - [✓] `SM_UPDATE_PLAYER_APPEARANCE` — `writeD(0)` for enchant/authorize bonus field replaced with `writeD(item.EnchantLevel >= 15 ? 1 : 0)`; zone peers now see weapon glow on players with +15 enchanted gear; matches Java SM_UPDATE_PLAYER_APPEARANCE.writeImpl() and the formula already used in SM_PLAYER_INFO line 84
    - [✓] `CM_USE_ITEM.HandleRecipeBookAsync` — recipe books now broadcast `SM_ITEM_USAGE_ANIMATION` to self and zone peers after learning the recipe, matching the behavior already present in `HandleSkillBookAsync`; previously recipe book use was silent from the perspective of other players
    - [✓] `LootService` — added `ConcurrentDictionary<int, Position> _positions`; `GenerateDrops(npc)` now stores `npc.Position`; `GetLootPosition(npcObjectId)` exposes it; `ClearLoot` removes from both dictionaries
    - [✓] `CM_START_LOOT` — added `MaxLootDistance = 10f` guard; if the loot pile position is known and the player is more than 10 units away, sends `SM_LOOT_STATUS.Locked` and returns; prevents looting corpses from across the map; lootPos is `Position?` (value type), `.Value` used for DistanceTo call
    - Build: 0 warnings, 0 errors

76. [✓] SM_ITEM_USAGE_ANIMATION + CM_USE_ITEM zone broadcast (session 2026-05-01)
    - [✓] `SM_ITEM_USAGE_ANIMATION` (0xB7) — new server packet; wire format: `D(playerObjId)+D(targetObjId)+D(itemObjId)+D(itemId)+D(time)+C(end)+C(0)+C(1)+D(unk)+C(0)`; `end=1` for instant-use items, `time=0` for no cast animation; matches Java SM_ITEM_USAGE_ANIMATION.writeImpl() (4.5 opcode)
    - [✓] `CM_USE_ITEM` — added `PlayerConnectionRegistry` field; broadcasts `SM_ITEM_USAGE_ANIMATION` to self + zone peers immediately before applying consumable effects (HP/MP elixirs) and before the skill-book learn notification; zone peers now see the item use animation when another player uses a potion or skill book
    - [✓] `GsPacketHandlerFactory` — 0xC7 now passes `_connRegistry` as fifth argument to CM_USE_ITEM
    - Build: 0 warnings, 0 errors

75. [✓] NPC chase behavior + CM_GM_BOOKMARK implementation (session 2026-05-01)
    - [✓] `NpcAiService` — added `ChaseSpeed=6.0f` and `MeleeRange=2.5f` constants; NPCs now move toward their target when outside melee range (sends `SM_MOVE.StartNpcMove` with 6 m/s chase speed, tracks arrival via `_chaseState`); only attack when within 2.5 units; when target is lost or dead, `StopChaseAsync` sends a return-home move; aggro scan now uses NPC's current position (not HomePosition) so wandering NPCs detect nearby players correctly; `_chaseState` dict cleaned up on NPC death/target-loss
    - [✓] `CM_GM_BOOKMARK` (0x11E) — was a no-op stub; now reads `"COMMAND playerName"` string from GM panel right-click menu; implements `TELEPORTTO` (GM teleports to named player's position) and `RECALL` (summons named player to GM's position); handles cross-zone delete/re-introduce broadcasts; mirrors `.goto`/`.summon` from CM_GM_COMMAND_SEND but triggered via client UI rather than chat command
    - [✓] `GsPacketHandlerFactory` — 0x11E now passes `conn, _connRegistry` to CM_GM_BOOKMARK
    - Build: 0 warnings, 0 errors

74. [✓] CM_ENTER_WORLD title IDs fix (session 2026-05-01)
    - [✓] `CM_ENTER_WORLD` — lines 181-182 were hardcoded to send `SM_TITLE_INFO.ActiveTitle(-1)` and `SM_TITLE_INFO.BonusTitle(-1)`, ignoring persisted title IDs; changed to `SM_TITLE_INFO.ActiveTitle(player.TitleId)` and `SM_TITLE_INFO.BonusTitle(player.BonusTitleId)` so titles appear correctly after login
    - Build: 0 warnings, 0 errors

73. [✓] /loc command + CM_MOTION read-format fix (session 2026-04-30)
    - [✓] `SM_SYSTEM_MESSAGE` — added `LocationDesc(worldId, x, y, z)` factory (code 230038 = STR_CMD_LOCATION_DESC); formats x/y/z as "F2" float strings matching Java's String.valueOf(float) output for typical coordinates
    - [✓] `CM_CLIENT_COMMAND_LOC` (0x12C) — reads nothing; sends `SM_SYSTEM_MESSAGE.LocationDesc` with player's current position; wired in factory with conn
    - [✓] `CM_MOTION` (0x2E5) — fixed Read() format: was reading H+C (motionId+speed), corrected to C+H+C (unk+motionId+motionType) matching Java CM_MOTION.readImpl(); RunAsync remains a no-op (motions system not yet implemented); fixing the read prevents consuming wrong bytes from subsequent packets
    - Build: 0 warnings, 0 errors

72. [✓] /roll command, dialog close heading, stance deactivate (session 2026-04-30)
    - [✓] `SM_SYSTEM_MESSAGE` — refactored `_param: string` to `_params: string[]` + `params string[]` ctor; updated `Write` to loop over all params; added `RollSelf(roll,max)` (code 1400126) and `RollOther(name,roll,max)` (code 1400127) factory methods; no wire-format change for existing single-param calls
    - [✓] `CM_CLIENT_COMMAND_ROLL` (0x109) — reads maxRoll(D), generates Random.Shared.Next(1, maxRoll+1), sends `SM_SYSTEM_MESSAGE.RollSelf` to self + `SM_SYSTEM_MESSAGE.RollOther` broadcast to zone peers; wired in factory with conn + connRegistry
    - [✓] `SM_HEADING_UPDATE` (opcode 0x39) — new packet: D(objectId) + C(heading); resets NPC facing direction on client after dialog close
    - [✓] `CM_CLOSE_DIALOG` (0x117) — reads targetObjectId(D), looks up NPC in world, fires Task.Run with 1200ms delay then sends `SM_HEADING_UPDATE`; wired with conn + world
    - [✓] `SM_PLAYER_STANCE` (opcode 0x1F) — new packet: D(objectId) + C(state); state 0=off, 1=active
    - [✓] `CM_TOGGLE_SKILL_DEACTIVATE` (0xE0) — reads skillId(H), sends `SM_PLAYER_STANCE(player.ObjectId, 0)` to self; stance effect system not yet implemented but client visual stance is cleared immediately; wired with conn
    - Build: 0 warnings, 0 errors

71. [✓] CM_PLAYER_SEARCH + SM_PLAYER_SEARCH — player search UI (session 2026-04-30)
    - [✓] `SM_PLAYER_SEARCH` (opcode 0xD3) — new server packet; wire format: `H(count)` + per player: `D(worldId) + F(x) + F(y) + F(z) + C(classId) + C(genderId) + C(level) + C(groupStatus) + S(name,56)`; groupStatus: 0=solo, 3=in group; matches Java SM_PLAYER_SEARCH.writeImpl()
    - [✓] `CM_PLAYER_SEARCH` (0x17D) — rewritten from no-op stub; reads fixed-width UTF-16LE name field (52 chars = 104 bytes via `ReadB`), region(D), classMask(D), minLevel(C), maxLevel(C), lfgOnly(C), padding(C); filters `connRegistry.GetAll()` with name substring, worldId, class bitmask, level range, LFG-only constraints; caps results at 30; sends `SM_PLAYER_SEARCH(results)` to the searcher
    - [✓] `GsPacketHandlerFactory` — 0x17D registration updated from `new CM_PLAYER_SEARCH()` to `new CM_PLAYER_SEARCH(conn, _connRegistry)`
    - Build: 0 warnings, 0 errors

68. [✓] SM_PING_RESPONSE — CM_PING_REQUEST implementation (session 2026-04-30)
    - [✓] `SM_PING_RESPONSE` (opcode 0x80) — new server packet; wire format: single byte `0x04`; matches Java SM_PING_RESPONSE.writeImpl()
    - [✓] `CM_PING_REQUEST` (0x105) — rewritten from no-op stub: injects `GsClientConnection`, responds with `SM_PING_RESPONSE`; clients send periodic pings and expect this response to avoid detecting the connection as dead
    - [✓] `GsPacketHandlerFactory` — CM_PING_REQUEST now receives `conn`; CM_PING (0xCE) was already implemented with SM_PONG (verified)
    - Build: 0 warnings, 0 errors

67. [✓] Broadcast robustness sweep 4 — final unprotected zone sends (session 2026-04-30)
    - [✓] `CM_LOOT_ITEM` — SM_LOOT_STATUS Close broadcast wrapped in try/catch; the loot-close notification now survives a disconnecting peer without truncating the broadcast to remaining zone players
    - [✓] `CM_OPEN_STATICDOOR` — SM_EMOTION(OPEN_DOOR) zone broadcast wrapped in try/catch
    - [✓] `CM_TARGET_SELECT` — SM_TARGET_UPDATE zone broadcast wrapped in try/catch
    - [✓] `CM_CUSTOM_SETTINGS` — SM_CUSTOM_SETTINGS zone broadcast wrapped in try/catch
    - [✓] `CM_EQUIP_ITEM` — SM_UPDATE_PLAYER_APPEARANCE zone broadcast wrapped in try/catch
    - [✓] `CM_GM_COMMAND_SEND.HandleHeal` — SM_ATTACK_STATUS HP+MP peer broadcast wrapped in try/catch (both sends are inside a single try block)
    - [✓] `CM_GM_COMMAND_SEND.HandleTeleport` — SM_DELETE departure broadcast and SM_PLAYER_INFO destination broadcast both wrapped in try/catch
    - All broadcast loops in ClientPackets/ and Services/ are now protected. Broadcast robustness sweep complete.
    - Build: 0 warnings, 0 errors

66. [✓] Broadcast robustness sweep 3 — group/revive/teleport try/catch (session 2026-04-30)
    - [✓] `CM_INVITE_TO_GROUP` — existing-member → new-player and new-player → existing-member `SM_GROUP_MEMBER_INFO(Enter)` sends now wrapped in `try { } catch { }` so a disconnecting member cannot abort the loop introducing the new player to remaining members
    - [✓] `CM_GROUP_DISTRIBUTION` — per-member `SM_GROUP_INFO` broadcast wrapped in `try { } catch { }`; a disconnecting member no longer aborts loot-settings delivery to remaining group members
    - [✓] `CM_REVIVE` — zone-departure `SM_DELETE` broadcast wrapped in try/catch; same-zone `SM_EMOTION(RESURRECT)` and `SM_EMOTION(STAND)` self-send and peer loops all wrapped in try/catch; mirrors the pattern established in Items 64–65
    - [✓] `CM_TELEPORT_SELECT` — departure `SM_DELETE` broadcast and post-teleport same-map peer `SM_PLAYER_INFO` broadcast both wrapped in try/catch
    - Build: 0 warnings, 0 errors

60. [✓] Group management commands — leave/kick/leader-change; CM_INVITE_TO_GROUP inviteType byte; CM_QUESTION_RESPONSE read format (session 2026-04-30)
    - [✓] `CM_INVITE_TO_GROUP` — added `_inviteType = r.ReadC()` before `_targetName = r.ReadS()` to match Java wire format; missing ReadC caused name to be read from wrong offset (first byte of name was the inviteType byte), corrupting all group invite lookups by name; added `if (_inviteType != 0) return` guard so alliance/league invite codes (12, 28) are silently ignored rather than creating a group
    - [✓] `CM_QUESTION_RESPONSE` — corrected Read() to match Java wire format: `D(questionId) + C(response) + C(unk) + H(unk) + D(senderId) + D(unk) + H(unk)`; previous implementation read D+H+C which misidentified field roles; RunAsync remains a no-op since question responses (group invites, etc.) are auto-accepted server-side
    - [✓] `SM_LEAVE_GROUP_MEMBER` (0xF7) — new server packet; wire format `D(0) + C(0) + D(0x3F) + D(0) + H(0)` matches Java `SM_LEAVE_GROUP_MEMBER.writeImpl()`; sent to the leaving/kicked player to clear their group UI
    - [✓] `CM_PLAYER_STATUS_INFO` — fully implemented (was a stub reading only 1 DWORD); now reads C(commandCode) + D(playerObjId) + D(allianceGroupId) + D(secondObjectId); handles GROUP_REMOVE_MEMBER(6) for self-leave and leader-kick, GROUP_BAN_MEMBER(2) for forced removal, GROUP_SET_LEADER(3) for leader transfer; resolves target Player from group.Members (works even for offline players); sends SM_LEAVE_GROUP_MEMBER to the removed player, SM_GROUP_MEMBER_INFO(Leave) to remaining members, SM_LEAVE_GROUP_MEMBER + SM_GROUP_INFO(dissolution) to the sole survivor when group falls below 2 members
    - [✓] `PlayerGroup` — added `SetLeader(int objectId)` method; guards on `HasMember` before updating `LeaderObjectId`
    - [✓] `GsPacketHandlerFactory` — CM_PLAYER_STATUS_INFO now receives `conn`, `_connRegistry`, `_groupService` (was `new CM_PLAYER_STATUS_INFO()` with no deps)
    - Build: 0 warnings, 0 errors

69. [✓] PvP AP reward + immediate AP persistence on kill (session 2026-05-01)
    - [✓] `AbyssRankService` — added `PvPApGained[9]` and `PvPApLost[9]` static arrays from Java AbyssRankEnum (ranks 1–9); added `PvPWorldIds` HashSet covering Reshanta (400010000) + Balaurea (600010000–600070000) + Elyos/Asmodian abyss bases (210050000, 220070000)
    - [✓] `AbyssRankService.IsPvPMap(worldId)` — returns true for any world in PvPWorldIds; used as gate before PvP AP exchange
    - [✓] `AbyssRankService.CalculatePvPApGained(winner, defeated)` — mirrors Java StatFunctions.calculatePvpApGained: base = defeated rank's pointsGained; applies level-difference scaling (×0.1 if diff>4, ×0.65 if diff=4, ×0.85 if diff=3, ×1.1 if diff=-2, ×1.2 if diff≤-3)
    - [✓] `AbyssRankService.CalculatePvPApLost(winner, defeated)` — mirrors Java StatFunctions.calculatePvPApLost: base = defeated rank's pointsLost with same level-difference scaling; AP cannot go below 0 via `LoseAp`
    - [✓] `AbyssRankService.LoseAp(player, amount)` — new helper; subtracts AP clamped at 0 (rank never decreases from AP loss in current impl)
    - [✓] `CM_ATTACK` — added `IPlayerDao` dependency; in player-kill path: if `IsPvPMap(worldId) && player.Race != deadPlayer.Race`, grant `CalculatePvPApGained` to killer and deduct `CalculatePvPApLost` from victim, send `SM_ABYSS_RANK` to both, call `UpdateAbyssAsync` for both immediately; in NPC-kill path: after AP gain, call `UpdateAbyssAsync` so AP survives a server crash (was previously only saved on disconnect)
    - [✓] `CM_CASTSPELL` — same changes as CM_ATTACK for both player-kill and NPC-kill paths; `playerDao` captured in Task.Run closure alongside existing service locals
    - [✓] `GsPacketHandlerFactory` — passes `_playerDao` to CM_ATTACK and CM_CASTSPELL constructors
    - Build: 0 warnings, 0 errors

70. [✓] Quest fixed reward items + kinah + AP (session 2026-05-01)
    - Root cause: `QuestRewards` only had `Exp`, `Title`, `SelectableItems`; `<reward_item>` elements, `gold` attribute, and `reward_abyss_point` attribute were silently dropped on quest completion
    - [✓] `QuestTemplate.QuestRewards` — added `[XmlElement("reward_item")] List<RewardItem> RewardItems`, `[XmlAttribute("gold")] long Gold`, `[XmlAttribute("reward_abyss_point")] int RewardAbyssPoint`; new `RewardItem` class (ItemId + Count) mirrors `SelectableRewardItem`
    - [✓] `CM_DIALOG_SELECT` — added `IPlayerDao _playerDao` field + constructor parameter (needed for abyss persist); added `KinahItemId = 182400001` constant
    - [✓] `CM_DIALOG_SELECT.HandleQuestRewardAsync` — after selectable-item grant: (1) iterates `RewardItems`, stacks on existing inventory item or creates new, single `SaveAllAsync` + `SM_INVENTORY_ADD_ITEM(granted)` for all fixed items; (2) grants `Gold` kinah into kinah stack, `SaveAllAsync` + `SM_INVENTORY_ADD_ITEM([kinah])`; (3) calls `AbyssRankService.AddAp`, sends `SM_ABYSS_RANK`, persists via `UpdateAbyssAsync`
    - [✓] `GsPacketHandlerFactory` — passes `_playerDao` to `CM_DIALOG_SELECT` constructor at opcode 0x114
    - Build: 0 warnings, 0 errors

76. [✓] Cross-zone combat guard + melee range validation (session 2026-05-01)
    - Root cause: `CM_ATTACK` and `CM_CASTSPELL` resolved targets from the global world registry without checking WorldId; a player could attack a target in a different zone; additionally `CM_ATTACK` had no melee range limit
    - [✓] `CM_ATTACK` — added `MaxMeleeRange = 7.0f` constant (lenient for latency; retail melee is ~5m); after target resolution: `if (target.Position.WorldId != player.Position.WorldId) return` + `if (player.Position.DistanceTo(target.Position) > MaxMeleeRange) return`; both guards prevent cross-zone and out-of-range auto-attacks
    - [✓] `CM_CASTSPELL` Task.Run damage path — added `if (target.Position.WorldId != castWorldId) return` after target resolution (castWorldId captured before Task.Run from player's pre-cast position); spell damage cannot cross zone boundaries; note: spell range check omitted until `SkillTemplate` exposes a range attribute
    - Build: 0 warnings, 0 errors

75. [✓] Player auto-attack cooldown enforcement in CM_ATTACK (session 2026-05-01)
    - Root cause: `CM_ATTACK` allowed unlimited attacks per packet — a packet-speed exploit could deal unlimited damage with no timing gate
    - [✓] `Creature` — added `LastAttackTime: DateTime` (default `DateTime.MinValue`); all creatures (player + NPC) can track their last auto-attack timestamp; NPC AI already had its own cooldown tracking — this unifies the concept at the Creature level
    - [✓] `CM_ATTACK.RunAsync` — added auto-attack speed guard at the top: computes `now = DateTime.UtcNow`, returns if `(now - player.LastAttackTime).TotalMilliseconds < player.CurrentAttackSpeed` (default 1500ms); sets `player.LastAttackTime = now` on successful pass; `var combatNow = now` reuses the captured timestamp
    - Build: 0 warnings, 0 errors

74. [✓] Skill cooldown enforcement in CM_CASTSPELL (session 2026-05-01)
    - Root cause: `CM_CASTSPELL` only checked `player.Skills.IsPresent(skillId)` before allowing the cast; no cooldown validation existed; players could fire any skill as fast as they could send packets
    - [✓] `PlayerSkillEntry` — added `LastUsedAt: DateTime` (default `DateTime.MinValue`); `MarkUsed()` sets it to `DateTime.UtcNow`; `IsOnCooldown(cooldownMs)` returns true if `cooldownMs > 0` and elapsed time since `LastUsedAt` is less than the cooldown
    - [✓] `PlayerSkillList.GetEntry(skillId)` — new method: returns the matching `PlayerSkillEntry` from basic or stigma dict, or null; allows callers to modify the entry (e.g., call `MarkUsed`)
    - [✓] `CM_CASTSPELL.RunAsync` — moved template lookup before cast animation broadcast; added cooldown gate: if `skillEntry.IsOnCooldown(template.Cooldown)` returns, no animation or damage is processed; calls `skillEntry.MarkUsed()` immediately on cast start (before animation) so rapid-fire packets within the cooldown window are all rejected; cooldown is server-side only — client display is unchanged
    - Build: 0 warnings, 0 errors

73. [✓] Quest race filter + collect-item pickup tracking (session 2026-05-01)
    - [✓] `CM_DIALOG_SELECT.HandleQuestAcceptAsync` — added race restriction check: `template.Race != "PC_ALL"` AND `!string.Equals(template.Race, player.Race.ToString(), OrdinalIgnoreCase)` → return; prevents Elyos players accepting Asmodian-only quests and vice versa; previously `QuestTemplate.Race` was parsed but never validated
    - [✓] `QuestService.HandleItemAcquiredAsync` — new method: for each active START-state quest with collect requirements, checks if the acquired `itemId` matches any `CollectItem.ItemId`; if so calls `IsRewardReady`; on readiness transitions quest to REWARD, persists, sends SM_QUEST_ACTION(StepUpdate, REWARD) + SM_QUEST_LIST; enables pure collect quests (no kill objectives) to show the map indicator after the player has all items
    - [✓] `CM_LOOT_ITEM` — added `QuestService _questService` field + constructor parameter; after saving inventory and notifying client, calls `_questService.HandleItemAcquiredAsync(player, entry.ItemId, _conn, ct)` for non-kinah items; kinah is never a collect quest item so excluded
    - [✓] `GsPacketHandlerFactory` — passes `_questService` to `CM_LOOT_ITEM` at opcode 0x179
    - Build: 0 warnings, 0 errors

72. [✓] Quest REWARD state transition on objective completion (session 2026-05-01)
    - Root cause: quest status stayed START after completing kill objectives; client never received REWARD state signal so the "Return to NPC" map indicator never appeared; Java `QuestService` calls `updateQuestStatus` to set REWARD when all objectives are met
    - [✓] `QuestService.HandleNpcKillAsync` — added `if (entry.Status != QuestStatus.START) continue` guard so REWARD/COMPLETE quests are skipped; after updating kill vars, calls new `IsRewardReady(entry, template, player)` helper; if all objectives met, sets `entry.Status = QuestStatus.REWARD` before persisting; sends `SM_QUEST_LIST` update only on REWARD transition so the map indicator appears immediately
    - [✓] `QuestService.IsRewardReady` — static helper: iterates `QuestKills` checking `entry.GetVar(seq) >= count`; iterates `CollectItems` checking inventory count via `FindByItemId`; returns false on first unsatisfied condition, true when all pass
    - [✓] `CM_DIALOG_SELECT.HandleQuestRewardAsync` — added `entry.Status == QuestStatus.START`-gated validation block: re-validates kills and collect-items before granting rewards; when status is already REWARD, validation is skipped (was already satisfied at transition time)
    - Build: 0 warnings, 0 errors

71. [✓] Quest title reward on completion (session 2026-05-01)
    - Root cause: `QuestRewards.Title` is parsed (was added in M56) but never applied; quest completion silently dropped the title reward; Java `QuestService.giveReward()` calls `player.getTitleList().addTitle(titleId, true, 0)` where the second parameter means auto-equip
    - [✓] `CM_DIALOG_SELECT.HandleQuestRewardAsync` — after AP award, checks `template.Rewards?.Title >= 0`; sets `player.TitleId = titleId`, calls `UpdateTitleAsync`, sends `SM_TITLE_INFO.ActiveTitle(titleId)` to self, broadcasts `SM_TITLE_INFO.BroadcastTitle(player.ObjectId, titleId)` to zone peers (WorldId-filtered, try/catch per broadcast convention); mirrors Java auto-equip behavior
    - Build: 0 warnings, 0 errors

68. [✓] Warehouse split support in CM_SPLIT_ITEM (session 2026-05-01)
    - [✓] `CM_SPLIT_ITEM` — removed early-return guard that silently dropped all warehouse split/merge operations (`_sourceStorageType != 0 || _destinationStorageType != 0`); storage type 1 = personal warehouse, 0 = inventory
    - [✓] `CM_SPLIT_ITEM` — source and dest storage now resolved dynamically: `_sourceStorageType == 1 ? player.Warehouse : player.Inventory`; both `player.Inventory` and `player.Warehouse` are `PlayerInventory` so no cast needed
    - [✓] `CM_SPLIT_ITEM` — new item created on split now sets `StorageType = _destinationStorageType` so DB row has correct storage column from first save
    - [✓] `CM_SPLIT_ITEM` — `SaveStoragesAsync` helper: saves inventory (`SaveAllAsync`) when either endpoint is type 0, saves warehouse (`SaveWarehouseAsync`) when either endpoint is type 1; both saves issued when cross-storage split occurs
    - [✓] `CM_SPLIT_ITEM` — `SendUpdatesAsync` helper: sends `SM_WAREHOUSE_INFO(player.Warehouse.All)` for warehouse-side changes, `SM_INVENTORY_ADD_ITEM([items])` for inventory-side changes; matches the pattern used in CM_MOVE_ITEM
    - [✓] `CM_SPLIT_ITEM` — cross-storage zero-count split (move full stack) sends `SM_DELETE_ITEM` for source, refreshes source storage, then adds new item to dest storage — no duplicate-slot or stale-item corruption possible
    - Build: 0 warnings, 0 errors

72. [✓] NPC proximity enforcement for shop, warehouse, airline, and quest interactions (session 2026-05-01)
    - Root cause: all NPC interaction handlers accepted packets regardless of player distance — a client could buy, sell, open warehouse, or accept quests from across the map
    - [✓] `CM_BUY_ITEM.BuyFromShopAsync` — added `player.Position.DistanceTo(npc.Position) > MaxInteractRange` guard immediately after NPC null check; constant `MaxInteractRange = 10.0f` (lenient for latency; retail NPC interaction is ~5m)
    - [✓] `CM_BUY_ITEM.SellToShopAsync` — sell path never looked up the NPC; added NPC lookup (`GetNpcByObjectId`) at the start followed by combined null+distance guard
    - [✓] `CM_DIALOG_SELECT` — added `MaxInteractRange = 10.0f` constant; added distance guard in BUY, WAREHOUSE_OPEN, AIRLINE_SERVICE, `HandleQuestAcceptAsync`, and `HandleQuestRewardAsync`; WAREHOUSE_OPEN and quest handlers now look up the NPC by `_targetObjectId` before checking distance
    - Build: 0 warnings, 0 errors

73. [✓] CM_SHOW_DIALOG proximity check (session 2026-05-01)
    - Root cause: players could open NPC dialogs (bind, shop, quests) from any distance; Java only adds objects to the knownList when within visibility range, which serves as an implicit proximity gate that the .NET implementation lacked
    - [✓] `CM_SHOW_DIALOG` — added `MaxInteractRange = 10.0f` constant and proximity guard after NPC null check: `if (player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;`
    - Build: 0 warnings, 0 errors

74. [✓] Inventory capacity enforcement (session 2026-05-01)
    - Root cause: `PlayerInventory.Add()` had no slot limit; players could accumulate unlimited items past the 27-slot default cube capacity without server resistance
    - [✓] `PlayerInventory` — added `Capacity = 27` property, `BagSlotUsed` (count of non-equipped items), `HasFreeSlot` (BagSlotUsed < Capacity), and `CanReceive(itemId, maxStackCount)` (true when item will stack onto existing entry OR a free slot is available)
    - [✓] `CM_BUY_ITEM.BuyFromShopAsync` — added `player.Inventory.CanReceive(itemId, template.MaxStackCount)` guard before each item in the purchase loop; full-inventory items are silently skipped (kinah already deducted for items that fit)
    - [✓] `CM_LOOT_ITEM` — added `!player.Inventory.HasFreeSlot` guard for non-kinah, non-existing-stack items; on failure calls new `LootService.ReturnLoot()` to restore the entry and sends `SM_SYSTEM_MESSAGE.InventoryFull()` to the client
    - [✓] `LootService` — added `ReturnLoot(npcObjectId, index, entry)` which re-inserts the entry at the original index so loot window ordering is preserved
    - [✓] `SM_SYSTEM_MESSAGE` — added `InventoryFull()` factory method (msg code 1300042 = STR_UI_INVENTORY_FULL)
    - [✓] `CM_DIALOG_SELECT.HandleQuestRewardAsync` — added `CanReceive` guard before awarding selectable reward item and each fixed reward item; full-inventory items are skipped while quest still completes and other rewards still granted
    - Build: 0 warnings, 0 errors
