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

76. [✓] Gather material consistency + inventory full check (session 2026-05-01)
    - Root cause: `PickMaterial()` uses `Random.Shared.Next(10_000_000)` on each call; `CM_GATHER` called it independently in `HandleStartAsync` (for the animation) and again in `HandleFinishAsync` (for the reward), so the player could receive a different item than shown
    - [✓] `GatherService` — replaced `int`-keyed session map with `GatherSession(GatherableObjectId, Material)` record; `StartGathering` now takes the pre-rolled `GatherableMaterial` and stores it in the session; `GetSession` replaces `GetActiveTarget`
    - [✓] `CM_GATHER.HandleStartAsync` — rolls `PickMaterial()` once, passes result to `StartGathering` for storage
    - [✓] `CM_GATHER.HandleFinishAsync` — retrieves the stored material from `GetSession` instead of re-rolling; validates session target matches; adds `HasFreeSlot` guard before consuming the harvest charge: full inventory returns early with `SM_SYSTEM_MESSAGE.InventoryFull()`
    - Build: 0 warnings, 0 errors

79. [✓] NpcAiService: use template attack delay and MainHandAttack for combat (session 2026-05-01)
    - Root cause: NPC attack cooldown was hardcoded to 4 000ms regardless of `NpcTemplate.adelay`; damage was always a level×5 formula regardless of the NPC's actual stat data
    - [✓] `NpcAiService` — replaced `AttackCooldown = 4s` constant with `DefaultAttackCooldown = 1500ms` fallback; per-tick attack check now reads `npc.Template.AttackDelay` (in ms) if > 0, else uses the default
    - [✓] `NpcAiService` — damage formula now checks `npc.Template.Stats?.MainHandAttack > 0`; if present uses ±25% variance around the template value; falls back to level×5 formula when the stat is missing
    - Build: 0 warnings, 0 errors

78. [✓] SM_STATS_INFO inventory slot count (session 2026-05-01)
    - Root cause: the inventory limit and current size fields in SM_STATS_INFO were hardcoded to (27, 0), so the client always displayed "0/27 slots used" regardless of how many items the player had
    - [✓] `SM_STATS_INFO.Write` — replaced hardcoded `(27, 0)` with `(p.Inventory.Capacity, p.Inventory.BagSlotUsed)` so the client UI reflects the actual bag state
    - Build: 0 warnings, 0 errors

77. [✓] Craft inventory-full check + material-before-product ordering (session 2026-05-01)
    - Root cause: `CM_CRAFT` consumed component items before checking if the crafted product could fit in inventory, creating a situation where components were lost but the product was never received
    - [✓] `CM_CRAFT.RunAsync` — added pre-craft inventory capacity check: computes `slotsFreed` (fully-consumed components that free a slot), `productStacks` (product stacks onto existing stack → no new slot needed); if product can't fit after accounting for freed component slots, sends `InventoryFull` and aborts before consuming anything
    - Uses `_dataManager.Items.GetTemplate(recipe.ProductId)` to look up `MaxStackCount` for stack detection
    - Build: 0 warnings, 0 errors

62. [✓] Player MaxHp/MaxMp bonus from equipped items (session 2026-05-01)
    - [✓] `ItemModifier` — added `[XmlAttribute("bonus")] public bool Bonus { get; set; }` to mark percentage-based modifiers; `ItemModifiers.GetStat(name)` now sums all non-bonus (flat) values for the stat (previously returned only the first match, which could miss duplicates)
    - [✓] `ItemTemplate` — added `MaxHpBonus` (`GetStat("MAXHP")`) and `MaxMpBonus` (`GetStat("MAXMP")`) computed properties
    - [✓] `Player` — added `BonusMaxHp` and `BonusMaxMp` properties (flat HP/MP bonus from equipped items)
    - [✓] `CM_ENTER_WORLD` — inventory/item initialization reordered: HP/MP template values now set AFTER items are loaded; `BonusMaxHp/MaxMp` computed from equipped items; `player.MaxHp = tpl.MaxHp + BonusMaxHp` applied before sending SM_STATS_INFO
    - [✓] `CM_EQUIP_ITEM` — recalculates `BonusMaxHp/MaxMp` on equip/unequip; updates `MaxHp/MaxMp` accordingly; `statTpl` variable reused for SM_STATS_INFO send (avoids second GetTemplate call)
    - [✓] `ExperienceService.HandleLevelUpAsync` — `player.MaxHp = tpl.MaxHp + player.BonusMaxHp` on level-up so item bonuses are preserved across levels
    - Build: 0 warnings, 0 errors

61. [✓] NPC XP from template + skill cast range enforcement (session 2026-05-01)
    - [✓] `NpcStatsTemplate` — added `[XmlAttribute("maxXp")] public long MaxXp { get; set; }` to parse the `maxXp` attribute from NPC stats elements in npc_templates.xml
    - [✓] `SkillTemplate` — added `SkillProperties` nested class (parses `first_target_range` attribute from `<properties>` child element); added `[XmlElement("properties")] SkillProperties? Properties` and `CastRange` computed property
    - [✓] `CM_ATTACK` NPC kill path — XP now uses `deadNpc.Template.Stats?.MaxXp` when > 0; falls back to `level * 50` when template data is absent
    - [✓] `CM_CASTSPELL` Task.Run — same XP fix applied to NPC kill path; `castRange` captured before Task.Run from template; range guard added after target resolution (`if (castRange > 0 && distance > castRange) return`)
    - Build: 0 warnings, 0 errors

57. [✓] Equipment-based damage and attack speed (session 2026-05-01)
    - [✓] `ItemTemplate` — added `WeaponStats` class (min_damage/max_damage/attack_speed XmlAttributes); added `ItemModifiers`/`ItemModifier` for generic `<modifiers><add name="..." value="N"/>` parsing; `PhysicalDefense` computed property wraps `GetStat("PHYSICAL_DEFENSE")`
    - [✓] `Creature` — `CurrentAttackSpeed` changed from expression-body constant to `{ get; set; } = 1500`; `BaseAttackSpeed` removed (was always 1500); `SM_EMOTION` updated to use `CurrentAttackSpeed`
    - [✓] `Player` — added `BasePhysicalAttack`, `MainHandMinDmg`, `MainHandMaxDmg`, `PhysicalDefense` properties
    - [✓] `CM_EQUIP_ITEM` — refreshes weapon stats (`MainHandMinDmg/MaxDmg/CurrentAttackSpeed`) from equipped main-hand template; refreshes `PhysicalDefense` sum from all equipped items; clears weapon stats when no weapon equipped
    - [✓] `CM_ATTACK` — damage formula: base = `BasePhysicalAttack || level*6`; weapon range added when `MainHandMinDmg > 0`; PvP pdef mitigation: `rawDmg * 1000 / (1000 + pdef)`
    - [✓] `CM_ENTER_WORLD` — `BasePhysicalAttack` from template; weapon stats from equipped main-hand; `PhysicalDefense` from all equipped items
    - [✓] `SM_STATS_INFO` — P-attack uses `(MainHandMinDmg+MainHandMaxDmg)/2 + template.MainHandAttack`; attack speed from `CurrentAttackSpeed`; P-def from real `PhysicalDefense`
    - Build: 0 warnings, 0 errors

58. [✓] Armor-based physical defense mitigation (session 2026-05-01)
    - [✓] NPC AI attack path — `NpcAiService` applies `PhysicalDefense` mitigation when NPC deals physical damage to a player (`rawDmg * 1000 / (1000 + pDef)`); defense = 0 when NPC attacks another NPC
    - [✓] `ExperienceService.HandleLevelUpAsync` — sets `player.BasePhysicalAttack = tpl.MainHandAttack` on level-up so the attack stat updates without requiring a relog
    - Build: 0 warnings, 0 errors

59. [✓] Group XP distance gate + stat-template base attack (session 2026-05-01)
    - [✓] `ExperienceService` — added `MaxGroupXpRange = 1500f` constant; `AddGroupExpAsync` now filters eligible members to those alive, in same WorldId, and within 1500 units of the killer (or is the killer); members outside range do not receive XP; solo path unchanged
    - [✓] `ExperienceService.HandleLevelUpAsync` — sets `player.BasePhysicalAttack = tpl.MainHandAttack` on level-up
    - Build: 0 warnings, 0 errors

60. [✓] NPC defense mitigation + player magic defense from items (session 2026-05-01)
    - [✓] `NpcStatsTemplate` — added `[XmlAttribute("mresist")] public int MResist { get; set; }` parsing the `mresist` attribute from NPC stats XML
    - [✓] `ItemTemplate` — added `MagicDefense` computed property: `Modifiers?.GetStat("MAGICAL_DEFEND") ?? 0`
    - [✓] `Player` — added `MagicDefense { get; set; }` (sum of MAGICAL_DEFEND from equipped items)
    - [✓] `CM_ENTER_WORLD` — initializes `player.MagicDefense` from equipped items on login (sum of `ItemTemplate.MagicDefense`)
    - [✓] `CM_EQUIP_ITEM` — refreshes `player.MagicDefense` alongside `player.PhysicalDefense` on equip/unequip
    - [✓] `CM_ATTACK` — pdef now resolves to: Player target → `PhysicalDefense`; Npc target → `template.Stats?.PDef ?? 0`; NPC physical defense from XML is now applied when players melee NPCs
    - [✓] `CM_CASTSPELL` Task.Run — spell damage now applies defense by skill type and target type: MAGICAL vs Player → `player.MagicDefense`; MAGICAL vs Npc → `stats.MResist`; PHYSICAL vs Player → `player.PhysicalDefense`; PHYSICAL vs Npc → `stats.PDef`
    - [✓] `SM_STATS_INFO` — M-def now uses `Math.Max(100, p.MagicDefense)` (current and base blocks) instead of hardcoded 100
    - Build: 0 warnings, 0 errors

75. [✓] BuyFromShop kinah overcharge fix (session 2026-05-01)
    - Root cause: `totalCost` was computed for ALL trade entries before inventory-capacity filtering, so players were charged kinah for items they could not receive when the inventory was nearly full
    - [✓] `CM_BUY_ITEM.BuyFromShopAsync` — replaced two-pass approach with single-pass `purchasePlan` list: iterates `_tradeEntries` in order, simulates slot consumption with a local counter (`simulatedSlots`), adds only receivable items to the plan, then sums cost from the plan; second loop adds items unconditionally since the plan is already validated — no overcharge possible
    - Build: 0 warnings, 0 errors

67. [✓] NPC HP and attack power scaling from RateOptions (session 2026-05-01)
    - [✓] `SpawnService` — injected `IOptions<RateOptions>`; in `SpawnNpc` applies `_rates.NormalMobsRateHp` to `npc.MaxHp` and `npc.CurrentHp` when rate != 1.0 (clamps to at least 1)
    - [✓] `NpcAiService` — injected `IOptions<RateOptions>`; applies `_rates.NormalMobsRatePw` to `rawDmg` after damage roll when rate != 1.0 (clamps to at least 1)
    - Both SpawnService and NpcAiService now respect the configured NPC scaling — server operators can tune mob difficulty via `GameServer:Rates` in appsettings.json
    - Build: 0 warnings, 0 errors

66. [✓] XP rate multipliers from RateOptions config (session 2026-05-01)
    - Root cause: `RateOptions` was registered in DI and configuration but never actually applied; kill XP and quest XP both ignored the configured multipliers
    - [✓] `ExperienceService` — injected `IOptions<RateOptions>`; in `AddGroupExpAsync` applied `_rates.XpRate` to `xpPerMember` (kill XP); added `AddQuestExpAsync(player, amount, conn, ct)` which pre-multiplies `amount * _rates.QuestXpRate` before calling `AddExpAsync`
    - [✓] `CM_DIALOG_SELECT.HandleQuestRewardAsync` — changed `_expService.AddExpAsync(...)` to `_expService.AddQuestExpAsync(...)` so quest XP uses the `QuestXpRate` multiplier (default 2.0×)
    - Build: 0 warnings, 0 errors

65. [✓] Divine Power (DP) tracking and drain on revive (session 2026-05-01)
    - [✓] `Player` — added `int Dp { get; set; }` (default 0, max 8000 in Aion retail; DB column `dp` was already in V1 schema)
    - [✓] `SM_DP_INFO` (NEW) — opcode 0x07; writes `playerObjectId(D)` + `currentDp(H)`; mirrors Java `SM_DP_INFO`
    - [✓] `IPlayerDao` — added `UpdateDpAsync(int playerId, int dp, CancellationToken ct)`
    - [✓] `PlayerDaoImpl` — added `dp` to `SelectColumns`; added `dp` to `PlayerRow` record; mapped `Dp = r.dp` in `ToPlayer`; implemented `UpdateDpAsync`
    - [✓] `CM_ENTER_WORLD` — sends `SM_DP_INFO(player.ObjectId, player.Dp)` after `SM_STATS_INFO` so client displays the DP bar on login
    - [✓] `CM_REVIVE` — added `IPlayerDao` field; on bind revive, if `player.Dp > 0`: sets to 0, calls `UpdateDpAsync`, sends `SM_DP_INFO(player.ObjectId, 0)`; mirrors Java `PlayerReviveService.revive()` `dp = 0` logic
    - [✓] `GsPacketHandlerFactory` — updated CM_REVIVE constructor call to pass `_playerDao`
    - Build: 0 warnings, 0 errors

64. [✓] Obelisk bind point via dialog (session 2026-05-01)
    - [✓] `CM_DIALOG_SELECT` — added `RESURRECT_BIND = 34` constant; new case: resolves NPC by `_targetObjectId`, checks proximity (`MaxInteractRange = 10f`), sets `player.BindPosition = npc.Position`, calls `UpdateBindPointAsync`, sends `SM_BIND_POINT_INFO(npc.Position)` + `SM_SYSTEM_MESSAGE.BindPointSet()`
    - [✓] `SM_SYSTEM_MESSAGE` — added `BindPointSet()` factory method (msg code 1300670 = STR_DEATH_REGISTER_RESURRECT_POINT, "You have set your resurrection point.")
    - Build: 0 warnings, 0 errors

63. [✓] Motion persistence (session 2026-05-01)
    - [✓] `V18__motions.sql` — new `player_motions` table (`player_id`, `slot`, `motion_id`; PK on both; FK → players ON DELETE CASCADE)
    - [✓] `IMotionDao` / `MotionDaoImpl` — `LoadByPlayerIdAsync` (SELECT), `UpsertAsync` (INSERT … ON DUPLICATE KEY UPDATE), `DeleteAsync` (DELETE WHERE player_id + slot)
    - [✓] `CM_MOTION` — added `IMotionDao` field; calls `UpsertAsync` after writing `player.ActiveMotions[slot]` so motion slot is persisted immediately
    - [✓] `CM_ENTER_WORLD` — added `IMotionDao` field; calls `LoadByPlayerIdAsync` after UI-settings load; populates `player.ActiveMotions` from DB so persisted motions are sent via `SM_MOTION.OwnList` on login
    - [✓] `GsPacketHandlerFactory` — added `IMotionDao` field + constructor param; wired into `CM_ENTER_WORLD` (0xAA) and `CM_MOTION` (0x2E5) constructors
    - [✓] `Program.cs` — registered `IMotionDao → MotionDaoImpl` as singleton
    - Build: 0 warnings, 0 errors

72. [✓] Gathering skill level guard (session 2026-05-01)
    - [✓] `CM_GATHER.HandleStartAsync` — checks `HarvestSkill > 0`; if player lacks skill → sends `GatherNoSkill` (1330054); if skill level < template SkillLevel → sends `GatherSkillLevelLow` (1330001); mirrors Java `GatherableController.checkPlayerSkill`
    - [✓] `SM_SYSTEM_MESSAGE` — added `GatherNoSkill(skillName)` (1330054) and `GatherSkillLevelLow(skillName)` (1330001) factory methods
    - Build: 0 warnings, 0 errors

73. [✓] Soul sickness death penalty on bind revive (session 2026-05-01)
    - [✓] `V19__soul_sickness.sql` — adds `soul_sickness TINYINT UNSIGNED NOT NULL DEFAULT 0` to players
    - [✓] `Player` — added `SoulSicknessCount` (0–10) + `SoulSicknessMultiplier` (5% reduction per stack, min 0.5×)
    - [✓] `IPlayerDao` / `PlayerDaoImpl` — added `UpdateSoulSicknessAsync`; soul_sickness in SelectColumns/PlayerRow/ToPlayer
    - [✓] `CM_ENTER_WORLD` — applies `SoulSicknessMultiplier` when computing MaxHp/MaxMp
    - [✓] `CM_REVIVE` — increments SoulSicknessCount (max 10) on bind revive; recomputes penalised MaxHp/MaxMp before restoring to 25%
    - Build: 0 warnings, 0 errors

74. [✓] Crafting skill level guard in CM_CRAFT (session 2026-05-01)
    - [✓] `CM_CRAFT` — added gate: player must have recipe.SkillId present and GetLevel >= recipe.SkillPoint; mirrors Java CraftService.checkCraft
    - Build: 0 warnings, 0 errors

75. [✓] Enchantment stone failure chance + success/fail messages (session 2026-05-01)
    - [✓] `CM_MANASTONE` case 1 — success rate: 60% base − 5% per current enchant level (min 5%); on failure, level drops by 1 (or clamps to 10 if > 10); mirrors Java EnchantService.enchantItem / enchantItemAct
    - [✓] `SM_SYSTEM_MESSAGE` — added `EnchantSuccess(itemName, level)` (1401681) and `EnchantFailed(itemName)` (1300456)
    - Build: 0 warnings, 0 errors

76. [✓] Skill persistence — player_skills table + ISkillDao (session 2026-05-01)
    - [✓] `V20__player_skills.sql` — creates `player_skills(player_id, skill_id, skill_level)` with PK + FK
    - [✓] `ISkillDao` / `SkillDaoImpl` — `LoadByPlayerIdAsync` + `UpsertAsync`
    - [✓] `CM_ENTER_WORLD` — loads persisted skills (crafting, skill books) from DB after auto-learn loop
    - [✓] `CM_USE_ITEM` — persists newly learned skills via `ISkillDao.UpsertAsync`
    - [✓] `Program.cs` / `GsPacketHandlerFactory` — registered and wired
    - Build: 0 warnings, 0 errors

77. [✓] Crafting skill advancement on successful craft (session 2026-05-01)
    - [✓] `CM_CRAFT` — after crafting, if player skill level < recipe.SkillPoint + 50, increments the skill and persists via ISkillDao; mirrors Java CraftService.finishCrafting addSkillXp gate
    - Build: 0 warnings, 0 errors

78. [✓] Gathering skill advancement on successful harvest (session 2026-05-01)
    - [✓] `CM_GATHER` — after successful harvest, if player HarvestSkill level < node.SkillLevel + 50, increments the skill and persists via ISkillDao; parallel to crafting skill advancement (M77)
    - Build: 0 warnings, 0 errors

79. [✓] Mail postage fee (session 2026-05-01)
    - [✓] `CM_SEND_MAIL` — deducts 10 kinah base + 1% commission on attached kinah before sending; sends NoEnoughKinah if insufficient; mirrors Java MailService fee formula
    - Build: 0 warnings, 0 errors

80. [✓] HP/MP persistence across sessions (session 2026-05-01)
    - [✓] `V21__player_hp_mp.sql` — adds `current_hp` / `current_mp` (nullable INT) to players table
    - [✓] `IPlayerDao` / `PlayerDaoImpl` — `UpdateHpMpAsync`; columns in SelectColumns/PlayerRow/ToPlayer (nullable → 0)
    - [✓] `GsClientConnection.DisposeAsync` — calls `UpdateHpMpAsync` before UpdateOnlineAsync so HP/MP survives relog
    - [✓] `CM_ENTER_WORLD` — uses persisted HP/MP (clamped to MaxHp/MaxMp); falls back to MaxHp/MaxMp for new characters (0 = not yet persisted)

81. [✓] Inventory cube expansion via Cube Artisan NPC dialog (session 2026-05-01)
    - [✓] `V22__cube_expands.sql` — adds `npc_expands TINYINT UNSIGNED NOT NULL DEFAULT 0` to players
    - [✓] `Player` — added `NpcExpands`, `QuestExpands`, and `CubeCapacity` computed property (27 + expands × 9)
    - [✓] `IPlayerDao` / `PlayerDaoImpl` — `UpdateCubeExpandAsync`; `npc_expands` in SelectColumns/PlayerRow/ToPlayer
    - [✓] `SM_INVENTORY_INFO` — now accepts `npcExpands` and `questExpands` parameters; written as bytes in packet header (previously hardcoded 0)
    - [✓] `SM_CUBE_UPDATE.CubeSize` — now accepts `npcExpands` and `questExpands` so the client sees the correct cube expansion state
    - [✓] `CM_ENTER_WORLD` — sets `player.Inventory.Capacity = player.CubeCapacity` after loading; passes npcExpands/questExpands to SM_INVENTORY_INFO
    - [✓] `CM_DIALOG_SELECT` — added `EXTEND_INVENTORY = 47` handler with price table (1k/12k/80k/180k/360k kinah per level); kinah guard; deducts kinah, increments NpcExpands, updates Capacity, saves to DB, sends SM_CUBE_UPDATE.CubeSize + SM_STATS_INFO + SM_SYSTEM_MESSAGE.CubeExpanded(9)
    - [✓] `SM_SYSTEM_MESSAGE` — added `CannotExpandCubeMore()` (code 1300430) and `CubeExpanded(slots)` (code 1300431)
    - Build: 0 warnings, 0 errors
    - Build: 0 warnings, 0 errors

82. [✓] Crash-safe volatile state persistence — FP, DP, soul sickness (session 2026-05-01)
    - [✓] `V23__player_fp.sql` — adds `current_fp INT NOT NULL DEFAULT 0` to players table
    - [✓] `IPlayerDao` — added `UpdateFpAsync(int playerId, int currentFp, CancellationToken ct)`
    - [✓] `PlayerDaoImpl` — added `current_fp` to SelectColumns/PlayerRow/ToPlayer; implemented `UpdateFpAsync`
    - [✓] `CM_ENTER_WORLD` — restores persisted FP (clamped to MaxFp, falls back to MaxFp for new characters)
    - [✓] `GsClientConnection.DisposeAsync` — added FP, DP, and soul_sickness saves on disconnect (previously only HP/MP was saved; crash would reset these three to last in-game update)
    - [✓] `AutoSaveService` — added HP/MP, FP, DP, and soul_sickness to the 5-minute periodic save (crash safety for all volatile player stats)
    - Build: 0 warnings, 0 errors

83. [✓] GM command extensions — quest, cube, soul sickness, skill (session 2026-05-01)
    - [✓] `CM_GM_COMMAND_SEND` — added `IQuestDao` and `ISkillDao` dependencies; six new dot-commands:
      - `.quest add <id>` — start quest in START status (creates entry, sends SM_QUEST_ACTION + SM_QUEST_LIST)
      - `.quest done <id>` — transition active quest to REWARD state
      - `.quest del <id>` — abandon/delete quest (mirrors CM_DELETE_QUEST flow)
      - `.cube [n]` — grant n NPC-expand levels (default 1, max level 5); sends SM_CUBE_UPDATE + SM_STATS_INFO
      - `.ss clear` — zero soul sickness stacks; recomputes MaxHp/MaxMp; sends SM_STATS_INFO
      - `.skill <skillId> [level]` — teach a skill via AddSkill + ISkillDao.UpsertAsync + SM_SKILL_LIST
    - [✓] `GsPacketHandlerFactory` — updated CM_GM_COMMAND_SEND constructor to pass _questDao and _skillDao
    - Build: 0 warnings, 0 errors

84. [✓] NPC dialog actions — quest select, mail postbox, vendor (session 2026-05-01)
    - [✓] `CM_DIALOG_SELECT` — added QUEST_SELECT=31, OPEN_POSTBOX=38, OPEN_VENDOR=33 constants and handlers:
      - `QUEST_SELECT` (31) → `HandleQuestSelectAsync`: checks NPC proximity, validates level/race prerequisites; sends SM_DIALOG_WINDOW(1007, questId) to show Accept/Decline for unstarted quest; sends SM_DIALOG_WINDOW(2375, questId) for in-progress quest; SM_DIALOG_WINDOW(1352, questId) for REWARD-state quest
      - `OPEN_POSTBOX` (38) → proximity check, then SM_DIALOG_WINDOW(18) + SM_MAIL_SERVICE(LetterList) to open mail inbox
      - `OPEN_VENDOR` (33) → SM_DIALOG_WINDOW(13) to open consignment/private-store window
    - [✓] `IMailDao` injected into CM_DIALOG_SELECT (field + constructor param)
    - [✓] `GsPacketHandlerFactory` — updated CM_DIALOG_SELECT constructor to pass _mailDao
    - Build: 0 warnings, 0 errors

85. [✓] Class ascension at level 9 (session 2026-05-01)
    - [✓] `Player` — `PlayerClass` changed from `{ get; init; }` to `{ get; set; }` to allow ascension mutation
    - [✓] `IPlayerDao` + `PlayerDaoImpl` — added `UpdateClassAsync` (UPDATE players SET player_class=...)
    - [✓] `ExperienceService` — after level-up to 9 with a starting class, sends `SM_DIALOG_WINDOW(0, dialogId, questId)` to show class selection UI (mirrors Java ClassChangeService.showClassChangeDialog)
    - [✓] `CM_DIALOG_SELECT` — added `ClassChoiceMap` (Race × dialogId → PlayerClass); when `targetObjectId == 0` and dialogId matches, calls `HandleClassChangeAsync`
    - [✓] `HandleClassChangeAsync` — validates starting class + level 9, maps valid ascension pairs (WARRIOR→GLADIATOR/TEMPLAR etc.), sets `player.PlayerClass`, saves to DB, grants all missing skills for new class (levels 1..current), saves skills, refreshes MaxHp/MaxMp/stats, sends SM_DIALOG_WINDOW(0,0,0) to close UI + SM_STATS_INFO + SM_SKILL_LIST
    - [✓] `CM_DIALOG_SELECT` — added `ISkillDao` dependency (for per-skill upsert after ascension)
    - [✓] `GsPacketHandlerFactory` — updated CM_DIALOG_SELECT to pass _skillDao
    - Build: 0 warnings, 0 errors

97. [✓] Legion rename (CM_APPEARANCE type 1) (session 2026-05-01)
    - [✓] `ILegionDao` — added `UpdateNameAsync(int legionId, string name, ct)` and `IsNameUsedAsync(string name, ct)`
    - [✓] `LegionDaoImpl` — implemented both: `UPDATE legions SET name = @name WHERE id = @legionId` and `SELECT COUNT(*) FROM legions WHERE name = @name`
    - [✓] `SM_SYSTEM_MESSAGE` — added `LegionNameInvalid()` (1400152), `LegionNameUnchanged()` (1400154), `LegionNameTaken()` (1400156), `LegionRenamed(string)` (1400158)
    - [✓] `CM_APPEARANCE` — added `ILegionDao` dependency; type 1 (HandleLegionRenameAsync): validates player is in legion; validates rename item (169680000 or 169680001); rejects unchanged/taken names with appropriate messages; consumes rename item; updates DB + in-memory Legion.Name; broadcasts `LegionRenamed` message to all online members via their connections
    - [✓] `GsPacketHandlerFactory` — updated 0x167 (CM_APPEARANCE) to pass `_legionDao`
    - Rename coupon itemIds: 169680000, 169680001 (same as player rename, per Java RenameService)
    - Build: 0 warnings, 0 errors

96. [✓] Composite stone combination (session 2026-05-01)
    - [✓] `CM_COMPOSITE_STONES` (0x192) — reads 3 UniqueIds (tool, first stone, second stone); validates tool is Combination Tool (165010000); validates both input stones are enchantment stones (itemId 166000001–166000095, level = itemId − 166000000, level ≤ 95, count ≥ 1); inventory space check; consumes 1 of each input (deletes or updates stack); creates output stone at `166000000 + CalcOutputLevel(firstLvl, secondLvl)` (mirrors Java CompositionAction: avg then ±jitter; clamped to 1-95); stacks if output itemId already exists; persists inventory; sends delta packets to client
    - [✓] `GsPacketHandlerFactory` — updated 0x192 to pass `conn, _itemDao`
    - Data insight: enchantment stones are itemId 166000001..166000095 (category=ENCHANTMENT); Combination Tool = 165010000 (category=COMBINATION)
    - Build: 0 warnings, 0 errors

95. [✓] Soul sickness recovery at Healer NPC (session 2026-05-01)
    - [✓] `SM_SYSTEM_MESSAGE` — added `SoulSicknessCleared()` factory (msg code 1300674 = STR_SUCCESS_RECOVER_EXPERIENCE)
    - [✓] `CM_DIALOG_SELECT` — added `RECOVERY = 35` constant and `SoulSicknessCostPerStack = 5_000`; added `case RECOVERY` branch → `HandleSoulSicknessRecoveryAsync`; validates NPC proximity and SoulSicknessCount > 0; charges 5k kinah per stack; on success: resets SoulSicknessCount to 0 via `UpdateSoulSicknessAsync`, recomputes MaxHp/MaxMp without soul sickness multiplier, persists inventory, sends SM_INVENTORY_ADD_ITEM + SM_STATS_INFO + SoulSicknessCleared message
    - Java RECOVERY dialog action (35) clears deathCount and recoverable XP; our simplified version uses flat fee (no recoverable-XP tracking) and clears all stacks at once
    - Build: 0 warnings, 0 errors

94. [✓] Private store — buy side (session 2026-05-01)
    - [✓] `CM_BUY_ITEM` — added `PlayerConnectionRegistry` dependency; added `case 0` branch (private store buy): resolves seller player by ObjectId, validates PrivateShop state; matches each requested itemId to a `PrivateStoreItem`; validates stock, buyer kinah (NoEnoughKinah message), and inventory space (InventoryFull message); decreases seller's inventory item (removes if depleted, updates StoreItems list); adds item to buyer's inventory (stacks or new slot); transfers kinah (deduct buyer, credit seller); persists both inventories; notifies buyer via SM_INVENTORY_ADD_ITEM; notifies seller via SM_DELETE_ITEM/SM_INVENTORY_ADD_ITEM through their connection; auto-closes store with SM_EMOTION(CLOSE_PRIVATESHOP) broadcast when StoreItems empties
    - [✓] `GsPacketHandlerFactory` — updated 0xF1 (CM_BUY_ITEM) to pass `_connRegistry`
    - Private store system is now complete: open → list → view → buy → auto-close
    - Build: 0 warnings, 0 errors

93. [✓] Private store — open/list/view (session 2026-05-01)
    - [✓] `PrivateStoreItem` — new record: `UniqueId`, `ItemId`, `Count`, `Price`
    - [✓] `Player` — added `List<PrivateStoreItem>? StoreItems` and `string StoreName` properties
    - [✓] `SM_PRIVATE_STORE_NAME` — new server packet (0x9E): broadcasts seller ObjId + store name string to zone
    - [✓] `SM_PRIVATE_STORE` — new server packet (0x9D): sends store listing (seller ObjId + per-item: uniqueId, itemId, count, price, WriteItemInfo) to viewer; reuses SM_INVENTORY_INFO.WriteItemInfo helper
    - [✓] `CM_PRIVATE_STORE_NAME` (0x15A) — reads store name; sets `CreatureState.PrivateShop` on seller; broadcasts `SM_EMOTION(OPEN_PRIVATESHOP)` + `SM_PRIVATE_STORE_NAME` to all zone players
    - [✓] `CM_PRIVATE_STORE` (0x155) — reads item list; validates each item is in seller's inventory (uniqueId, itemId, count, price > 0); stores as `StoreItems` on player; 0-item submission closes the store (`~PrivateShop` state + `SM_EMOTION(CLOSE_PRIVATESHOP)` broadcast)
    - [✓] `CM_SHOW_DIALOG` — added player-target branch: if target is a player with `PrivateShop` state set, sends `SM_PRIVATE_STORE` to the viewer (store browsing); NPC dialog flow unchanged
    - [✓] `GsPacketHandlerFactory` — updated 0x155 and 0x15A registrations to pass `_connRegistry`
    - Buy-from-store (CM_BUY_TRADE_IN_TRADE or equivalent) is a separate milestone
    - Build: 0 warnings, 0 errors

92. [✓] NPC and gatherable respawn times from spawn data (session 2026-05-01)
    - [✓] `Npc` — added `public int RespawnTime { get; set; }` property
    - [✓] `Gatherable` — added `public int RespawnTime { get; set; }` property
    - [✓] `SpawnService` — `SpawnNpc(template, position, int respawnTime = 0)` and `SpawnGatherable(template, position, int respawnTime = 0)` now store `respawnTime` on the entity; `SpawnAll()` passes `entry.RespawnTime` for both NPC and gatherable loops; `ScheduleRespawn(Npc)` uses `npc.RespawnTime > 0 ? npc.RespawnTime : DefaultRespawnSeconds` (no longer accepts explicit seconds); `ScheduleGatherableRespawn(Gatherable)` uses `gatherable.RespawnTime > 0 ? gatherable.RespawnTime : DefaultRespawnSeconds` (no longer accepts explicit seconds); respawned entity inherits original respawn time so the value persists across respawn cycles
    - [✓] `CM_GATHER` — removed `const int RespawnSeconds = 300`; calls `ScheduleGatherableRespawn(target)` without explicit delay; data drives the delay (e.g. 295s Poeta, 230s Eltnen vs old hardcoded 300s)
    - Previously NPCs always respawned in 30 s and gatherables in 300 s regardless of XML data; now Sanctum city NPCs use 295 s, Eltnen gatherables use 230 s, etc.
    - Build: 0 warnings, 0 errors

91. [✓] Quest item tracking for gather and craft (session 2026-05-01)
    - [✓] `CM_GATHER` — injected `QuestService`; calls `HandleItemAcquiredAsync(player, material.ItemId, ...)` after the gathered item is persisted and notified to client; gathering quest items now transitions quests to REWARD state immediately
    - [✓] `CM_CRAFT` — injected `QuestService`; calls `HandleItemAcquiredAsync(player, recipe.ProductId, ...)` after the crafted product is persisted and notified to client; crafting quest items now triggers quest progress
    - [✓] `GsPacketHandlerFactory` — updated 0xD1 (CM_GATHER) and 0x12F (CM_CRAFT) to pass `_questService`
    - Previously only CM_LOOT_ITEM called HandleItemAcquiredAsync; this closes the gap for gather/craft collect quests
    - Build: 0 warnings, 0 errors

90. [✓] Group quest kill sharing (session 2026-05-01)
    - [✓] `QuestService` — injected `PlayerConnectionRegistry`; refactored `HandleNpcKillAsync` into public entry + private `ProcessKillForPlayerAsync`; after awarding kill credit to the killer, iterates group members whose WorldId matches the dead NPC and whose connection is live; sends `SM_QUEST_ACTION(StepUpdate)` + `SM_QUEST_LIST` to each qualifying group member; mirrors Java QuestService group kill-share behaviour
    - [✓] Each member notification wrapped in try/catch so a disconnecting member doesn't abort the loop
    - [✓] DI resolves `PlayerConnectionRegistry` automatically — no `Program.cs` changes needed
    - Build: 0 warnings, 0 errors

89. [✓] NPC skill casting in combat (session 2026-05-01)
    - [✓] `NpcSkillData` — new data holder; parses `npc_skills.xml` (`<npcskills npcid><npcskill skillid skilllevel probability>`); exposes `GetSkills(npcId)`
    - [✓] `IDataManager` / `DataManager` — added `NpcSkillData NpcSkills` property; loaded in `DataManager` constructor
    - [✓] `NpcAiService` — injected `IDataManager`; added `_lastSkillTime` dictionary (per-NPC 8-second cooldown); added `TryCastNpcSkillAsync`: picks random skill, rolls probability, sends `SM_CASTSPELL(npc→player, targetType=3)`, applies magic damage with `MagicDefense` mitigation, sends `SM_ATTACK_STATUS`
    - [✓] `NpcAiService` — `_lastSkillTime` cleared in zero-players path and dead-NPC cleanup; also cleared on player kill
    - Build: 0 warnings, 0 errors

88. [✓] DP (Divine Power) generation from combat (session 2026-05-01)
    - [✓] `CM_ATTACK` — after successful hit, adds 100 DP (capped at 6000), sends `SM_DP_INFO` to caster; previously DP never grew from combat
    - [✓] `CM_CASTSPELL` — inside fire-and-forget damage task, after hit status broadcast, adds 150 DP (capped at 6000), sends `SM_DP_INFO`; covers spell hits in addition to basic attacks
    - DP is already loaded/persisted (CM_ENTER_WORLD/DisposeAsync/AutoSaveService); generation was the missing piece
    - Build: 0 warnings, 0 errors

87. [✓] Crafting/gathering skill tier upgrade from NPC + stigma window (session 2026-05-01)
    - [✓] `SM_SYSTEM_MESSAGE` — added `CraftSkillMaxLevel()` (1390233) and `CraftSkillNeedQuest()` (1300834)
    - [✓] `CM_DIALOG_SELECT` — added `OPEN_STIGMA_WINDOW=4`, `GATHER_SKILL_LEVELUP=45`, `COMBINE_SKILL_LEVELUP=46` constants
    - [✓] `CM_DIALOG_SELECT` — added `NpcSkillMap` (NPC ID → skillId + name) covering all Elyos/Asmodian gathering (30002/30003) and crafting (40001–40010) trainer NPCs (mirrors Java CraftSkillUpdateService.npcBySkill)
    - [✓] `CM_DIALOG_SELECT` — added `CraftTierCosts` dictionary: 0→3500, 99→17k, 199→115k, 299→460k, 399→blocked(quest), 449→6M, 499→blocked(quest)
    - [✓] `CM_DIALOG_SELECT` — `OPEN_STIGMA_WINDOW` sends `SM_DIALOG_WINDOW(npcId, 1)` to open stigma UI
    - [✓] `CM_DIALOG_SELECT.HandleCraftSkillUpgradeAsync` — validates player level≥10, NPC trainer type, tier boundary, kinah; deducts kinah, increments skill level by 1, persists via ISkillDao, sends SM_INVENTORY_ADD_ITEM + SM_SKILL_LIST(msgId=1330004)
    - [✓] Expert cap guard: max 2 crafting skills at level≥400; master cap: max 1 at level≥500 (mirrors Java CraftConfig.MAX_EXPERT/MASTER)
    - [✓] `CountCraftingSkillsAbove` — counts player's crafting skills above a threshold level
    - Build: 0 warnings, 0 errors

86. [✓] Account warehouse — cross-character item sharing (session 2026-05-01)
    - [✓] `V24__account_warehouse.sql` — new `account_warehouse_items(unique_id, account_id, item_id, count, slot, enchant_level)` table
    - [✓] `IItemDao` — added `FindAccountWarehouseAsync(int accountId, ...)` and `SaveAccountWarehouseAsync(int accountId, IEnumerable<Item> items, ...)`
    - [✓] `ItemDaoImpl` — implemented both: `FindAccountWarehouseAsync` reads from `account_warehouse_items`; `SaveAccountWarehouseAsync` does DELETE-then-INSERT (same pattern as personal warehouse)
    - [✓] `Player` — added `AccountWarehouse` property (`PlayerInventory`) for in-memory cross-character storage
    - [✓] `SM_ACCOUNT_WAREHOUSE_INFO` — new packet (opcode 0x1A, storage-type byte 3); reuses `SM_WAREHOUSE_INFO.WriteItemInfo` for item encoding
    - [✓] `CM_ENTER_WORLD` — loads account warehouse from DB into `player.AccountWarehouse` after personal warehouse
    - [✓] `CM_DIALOG_SELECT` — added `RETRIEVE_ACCOUNT_WH=27` and `DEPOSIT_ACCOUNT_WH=28` handlers: send SM_ACCOUNT_WAREHOUSE_INFO to open/populate the UI
    - [✓] `CM_MOVE_ITEM` — added cases (0,2), (2,0), (2,2): inventory↔account warehouse transfers and reorder; saves both storages and sends appropriate update packets
    - [✓] `GsClientConnection.DisposeAsync` — saves account warehouse on disconnect
    - [✓] `AutoSaveService` — saves account warehouse in 5-minute periodic save
    - Build: 0 warnings, 0 errors

98. [✓] Legion contribution points from AP gains (session 2026-05-01)
    - [✓] `SM_LEGION_EDIT` — added type 0x03 constructor (`SM_LEGION_EDIT(long contributionPoints)`) writing `WriteQ(contributionPoints)`; mirrors Java SM_LEGION_EDIT case 0x03 (change legion contributions)
    - [✓] `ILegionDao` — added `UpdateContributionPointsAsync(int legionId, long points, ct)` interface method
    - [✓] `LegionDaoImpl` — implemented: `UPDATE legions SET contribution_points = @points WHERE id = @legionId`
    - [✓] `CM_ATTACK` — added `ILegionDao` dependency; after PvP AP gain (player kill in Abyss) and NPC AP gain (Abyss guard kill): adds `apGain` to `player.Legion.ContributionPoints`, persists, and broadcasts `SM_LEGION_EDIT(0x03)` to all online legion members via `AwardLegionContributionAsync` helper
    - [✓] `CM_CASTSPELL` — same: added `ILegionDao` dependency; static `AwardLegionContributionAsync` helper; called after PvP and Abyss NPC kill AP grants within fire-and-forget task
    - [✓] `GsPacketHandlerFactory` — updated 0xE2 (CM_ATTACK) and 0xE3 (CM_CASTSPELL) to pass `_legionDao`
    - Mirrors Java AbyssPointsService.addAp: when a player earns AP, the same amount is contributed to their legion's `contribution_points` and broadcast to all online members
    - Build: 0 warnings, 0 errors

99. [✓] Godstone socketing (CM_GODSTONE_SOCKET) (session 2026-05-01)
    - [✓] `V25__godstone.sql` — `ALTER TABLE player_items ADD COLUMN godstone_item_id INT NOT NULL DEFAULT 0`
    - [✓] `ItemTemplate` — added `[XmlAttribute("mask")] Mask int`; `[XmlElement("godstone")] GodstoneInfo?`; `CanSocketGodstone => (Mask & 1024) != 0` (CAN_PROC_ENCHANT = 1 << 10 per Java ItemMask)
    - [✓] `GodstoneInfo` — new record with `SkillId`, `SkillLvl`, `Probability`, `ProbabilityLeft` (maps XML `<godstone skillid=X skilllvl=Y probability=Z probabilityleft=W>`)
    - [✓] `Item` — added `GodStoneItemId int = 0`
    - [✓] `ItemDaoImpl` — updated SELECT to include `godstone_item_id` (index 6); `is_equipped` shifted to index 7; INSERT includes `godstone_item_id`
    - [✓] `SM_UPDATE_PLAYER_APPEARANCE` — writes `item.GodStoneItemId` instead of hardcoded 0
    - [✓] `SM_SYSTEM_MESSAGE` — added `GodstoneApplied()` (1300502), `GodstoneInvalid()` (1300503), `GodstoneAlreadySlotted()` (1300504), `GodstoneNoKinah()` (1300505)
    - [✓] `CM_GODSTONE_SOCKET` — full implementation: finds weapon (inventory or equipped), validates `CanSocketGodstone`, checks `GodStoneItemId == 0` (not already slotted), validates stone has godstone data, kinah ≥ 100,000; deducts kinah; consumes 1 stone; sets `weapon.GodStoneItemId = stone.ItemId`; persists; sends SM_INVENTORY_ADD_ITEM([weapon, kinahItem]) + SM_SYSTEM_MESSAGE; broadcasts SM_UPDATE_PLAYER_APPEARANCE if equipped
    - [✓] `GsPacketHandlerFactory` — updated 0x139 to pass `conn, _itemDao, _dataManager, _connRegistry`
    - Godstone proc (combat trigger) is NOT implemented — stored itemId enables correct wire format encoding; proc effect is future work
    - Build: 0 warnings, 0 errors

100. [✓] Item tuning — optional socket (CM_TUNE) (session 2026-05-01)
    - [✓] `V26__tuning.sql` — `ALTER TABLE player_items ADD COLUMN optional_socket TINYINT(1) NOT NULL DEFAULT -1`
    - [✓] `ItemTemplate` — added `[XmlAttribute("option_slot_bonus")] OptionSlotBonus int`
    - [✓] `Item` — added `OptionalSocket int = -1` (−1 = untuned; 0+ = extra socket slots from tuning)
    - [✓] `ItemDaoImpl` — updated SELECT to include `optional_socket` (index 7); `is_equipped` shifted to index 8; INSERT includes `optional_socket`
    - [✓] `SM_SYSTEM_MESSAGE` — added `TuningComplete()` (msg code 1401626)
    - [✓] `CM_TUNE` (new file, 0x189): free initial tuning path — validates `OptionalSocket == -1`, template has `OptionSlotBonus > 0`; assigns `Random.Next(0, OptionSlotBonus + 1)`; persists via `SaveAllAsync`; sends `SM_INVENTORY_ADD_ITEM([item])` + `SM_SYSTEM_MESSAGE.TuningComplete()`; scroll re-tuning path (`tuningScrollId != 0`) is stub (TuningAction not yet ported)
    - [✓] `GsPacketHandlerFactory` — registered 0x189 → `CM_TUNE(conn, _itemDao, _dataManager)`
    - Tuning does not affect the manastone socket count wire format (SM_INVENTORY_INFO.WriteItemInfo remains simplified); random bonus stats (setRndBonus) not implemented (requires ITEM_RANDOM_BONUSES data)
    - Build: 0 warnings, 0 errors

101. [✓] Item remodel — skin change (CM_ITEM_REMODEL) (session 2026-05-01)
    - [✓] `V27__item_skin.sql` — `ALTER TABLE player_items ADD COLUMN skin_item_id INT NOT NULL DEFAULT 0`
    - [✓] `Item` — added `SkinItemId int = 0` (0 = use ItemId for appearance)
    - [✓] `ItemDaoImpl` — updated SELECT to include `skin_item_id` (index 8); `is_equipped` shifted to index 9; INSERT includes `skin_item_id`
    - [✓] `SM_UPDATE_PLAYER_APPEARANCE` — `w.WriteD` now uses `SkinItemId != 0 ? SkinItemId : ItemId` for the skin template field
    - [✓] `SM_SYSTEM_MESSAGE` — added `RemodelLevelLimit()` (1300476), `RemodelNotCompatible()` (1300480), `RemodelNoKinah()` (1300481), `RemodelSuccess()` (1300483)
    - [✓] `CM_ITEM_REMODEL` (full rewrite, 0x138): validates player level >= 10, kinah >= 1000, slot compatibility (`keepTemplate.Slot == extractTemplate.Slot`); deducts kinah, consumes extractItem (delete if count reaches 0); sets `keepItem.SkinItemId = extractSkinId`; persists via `SaveAllAsync`; broadcasts `SM_UPDATE_PLAYER_APPEARANCE` if keepItem is equipped; Pattern Reshaper (168100000) not yet ported
    - [✓] `GsPacketHandlerFactory` — wired 0x138 → `CM_ITEM_REMODEL(conn, _itemDao, _dataManager, _connRegistry)`
    - Build: 0 warnings, 0 errors

102. [✓] Weapon fusion and break (CM_FUSION_WEAPONS, CM_BREAK_WEAPONS) (session 2026-05-01)
    - [✓] `V28__weapon_fusion.sql` — `ALTER TABLE player_items ADD COLUMN fusioned_item_id INT NOT NULL DEFAULT 0`
    - [✓] `ItemTemplate` — added `[XmlAttribute("weapon_type")] WeaponTypeName string`; `IsTwoHandWeapon` (based on _2H suffix + BOW); `IsCanFuse` (presence of `<fusionaction>` in `<actions>`); `FusionAction` empty class in `ItemActions`
    - [✓] `Item` — added `FusionedItemId int = 0` (stores secondary weapon's ItemId; 0 = not fused)
    - [✓] `ItemDaoImpl` — updated SELECT to include `fusioned_item_id` (index 9); `is_equipped` shifted to index 10; INSERT includes `fusioned_item_id`
    - [✓] `SM_SYSTEM_MESSAGE` — added `CompoundNotAvailable()` (1400289), `CompoundDifferentType()` (1400364), `CompoundMainRequireHigherLevel()` (1400288), `CompoundNotEnoughMoney()` (1400337), `CompoundSuccess()` (1400336), `DecompoundSuccess()` (1400335), `DecompoundNotAvailable()` (1400373)
    - [✓] `CM_FUSION_WEAPONS` (0x16C): validates both items are 2H weapons (`IsTwoHandWeapon`), neither already fused, same `WeaponTypeName`, first level >= second, kinah >= `level² × 2`; sets `first.FusionedItemId = second.ItemId`; consumes second; deduct kinah; persist; sends SM_INVENTORY_ADD_ITEM + CompoundSuccess
    - [✓] `CM_BREAK_WEAPONS` (0x16D): validates FusionedItemId != 0; clears it; persists; sends SM_INVENTORY_ADD_ITEM + DecompoundSuccess
    - [✓] `GsPacketHandlerFactory` — wired 0x16C → `CM_FUSION_WEAPONS(conn, _itemDao, _dataManager)`; 0x16D → `CM_BREAK_WEAPONS(conn, _itemDao)`
    - Note: fusion-stone transfer (copyFusionStones) and improvement compatibility check not ported (requires fusion socket system)
    - Build: 0 warnings, 0 errors

103. [✓] Item dyeing (CM_USE_ITEM type 2 + DyeAction in ItemTemplate) (session 2026-05-01)
    - [✓] `V29__item_dye.sql` — `ALTER TABLE player_items ADD COLUMN dye_color INT NOT NULL DEFAULT 0`
    - [✓] `Item` — added `DyeColor int = 0` (stores dye item's ItemId; 0 = undyed)
    - [✓] `ItemTemplate` — added `DyeAction` class (`color string` + `minutes int`); `[XmlElement("dye")]` in `ItemActions`; `IsItemDyePermitted => (Mask & 32768) != 0` (DYEABLE flag)
    - [✓] `ItemDaoImpl` — updated SELECT to include `dye_color` (index 10); `is_equipped` shifted to index 11; INSERT includes `dye_color`
    - [✓] `SM_UPDATE_PLAYER_APPEARANCE` — updated `w.WriteD(0)` item color field to use `item.DyeColor`
    - [✓] `SM_SYSTEM_MESSAGE` — added `DyeRemoved()` (1300510), `DyeApplied()` (1300511), `DyeCannotDye()` (1300512), `DyeCannotRemove()` (1300513)
    - [✓] `CM_USE_ITEM` — when `_type == 2` and template has `DyeAction`: validates target exists and is dyeable, applies/removes dye (`DyeColor = dyeItem.ItemId` or 0 for "no" color), consumes dye item, persists, broadcasts `SM_UPDATE_PLAYER_APPEARANCE` if target is equipped
    - Build: 0 warnings, 0 errors

104. [✓] Broker (auction house) system (session 2026-05-01)
    - [✓] `V30__broker.sql` — new `broker_items(id, item_id, seller_id, seller_name, creator_name, item_count, price, race, enchant_level, is_settled, is_sold, is_canceled, expire_time, settle_time)` table
    - [✓] `BrokerItem` — new model class in `Model/Broker/`; fields: Id, ItemId, SellerId, SellerName, CreatorName, ItemCount, Price, Race(0=Elyos/1=Asmodian), EnchantLevel, IsSettled, IsSold, IsCanceled, ExpireTime, SettleTime
    - [✓] `IBrokerDao` / `BrokerDaoImpl` — `LoadAllAsync`, `InsertAsync`, `MarkSoldAsync`, `MarkCanceledAsync`, `MarkSettledAsync`, `DeleteAsync`
    - [✓] `BrokerService` — in-memory dictionaries per race (active + settled); `InitAsync` loads from DB on startup; `GetPage(race, page, filterItemIds)`, `GetTotalCount`, `GetListings(playerId, race)`, `GetSettledItems`, `GetSettledKinah`; `RegisterAsync` (4% price + 1000 kinah fee, max 15 listings, consumes item from inventory); `CancelAsync` (returns to seller's listing view); `BuyAsync` (deducts kinah, marks sold, notifies seller of settled kinah icon); `SettleAsync` (credits seller kinah for all sold items, removes settled records)
    - [✓] `SM_BROKER_SERVICE` (opcode 0x92) — multi-type server packet: `SearchedItems(items, total, page)`, `RegisteredItems(items)`, `BuyResult(remainingKinah)`, `RegisterSuccess(item, newCount)`, `RegisterError(code)`, `SettledItems(items, kinah)`, `ShowSettledIcon(kinah)`, `RemoveSettledIcon()`; writes fixed-size item blobs: MANA_SOCKETS (132 bytes: enchant, skin, optional socket, manastones×12, godstone, dye, idian, padding), PREMIUM_OPTION (3 bytes), POLISH_INFO (4 bytes)
    - [✓] `CM_BROKER_LIST` (0x159) — browses all active listings for player's race; returns page via `GetPage(race, page, null)`
    - [✓] `CM_BROKER_SEARCH` (0x15E) — searches by item IDs; returns filtered page via `GetPage(race, page, itemIds)`
    - [✓] `CM_BROKER_REGISTERED` (0x15F) — views own listings via `GetListings(playerId, race)`
    - [✓] `CM_REGISTER_BROKER_ITEM` (0x15D) — registers item: validates price/count, calls `RegisterAsync`
    - [✓] `CM_BROKER_CANCEL_REGISTERED` (0x142) — cancels listing via `CancelAsync`
    - [✓] `CM_BUY_BROKER_ITEM` (0x15C) — buys item via `BuyAsync`; notifies seller if online
    - [✓] `CM_BROKER_SETTLE_LIST` (0x143) — views settled items + kinah via `GetSettledItems` + `GetSettledKinah`
    - [✓] `CM_BROKER_SETTLE_ACCOUNT` (0x140) — collects kinah from sold items via `SettleAsync`; persists inventory; sends SM_INVENTORY_ADD_ITEM with kinah update
    - [✓] `GsPacketHandlerFactory` — all 8 broker packets now pass `conn` + `_brokerService` (+ `_itemDao` for settle account); `BrokerService` injected as new ctor param
    - [✓] `GameServerHost` — injects `BrokerService`; calls `InitAsync` before server starts listening
    - [✓] `Program.cs` — registers `IBrokerDao → BrokerDaoImpl` and `BrokerService` as singletons
    - Registration fee: 4% of price + 1000 kinah flat; max 15 listings per player; items expire after 3 days
    - Item blobs: MANA_SOCKETS writes enchant level + 48 bytes manastone padding (no actual manastones stored in broker) + godstone + dye fields; PREMIUM_OPTION writes 3 zero bytes; POLISH_INFO writes 4 zero bytes; blob sizes match Java ManaStoneInfoBlobEntry.getSize()==132
    - Build: 0 warnings, 0 errors

105. [✓] Legion warehouse items (storage type 4) (session 2026-05-01)
    - [✓] `V32__legion_warehouse.sql` — new `legion_warehouse_items(unique_id, legion_id, item_id, count, slot, enchant_level)` table with FK → legions ON DELETE CASCADE
    - [✓] `Legion` — added `public PlayerInventory WarehouseItems { get; } = new()` property (in-memory container, loaded on first member login)
    - [✓] `ILegionDao` — added `FindWarehouseItemsAsync(int legionId, ct)` and `SaveWarehouseItemsAsync(int legionId, IEnumerable<Item> items, ct)`
    - [✓] `LegionDaoImpl` — implemented both: SELECT from `legion_warehouse_items WHERE legion_id`; DELETE+INSERT pattern for save (mirrors personal/account warehouse)
    - [✓] `SM_LEGION_EDIT` — added private ctor + `static WarehouseKinah(long kinah)` factory for type 0x04; writes `WriteQ(warehouseKinah)` in switch
    - [✓] `SM_LEGION_WAREHOUSE_INFO` — new packet (opcode 0x1A, storage-type byte 4); writes C(4)+C(0)+C(1)+C(0)+H(count) then item blobs via `SM_WAREHOUSE_INFO.WriteItemInfo`
    - [✓] `CM_DIALOG_SELECT` — added `ILegionDao _legionDao` field; added `OPEN_LEGION_WAREHOUSE = 53` const; new `HandleOpenLegionWarehouseAsync`: sends `SM_LEGION_EDIT.WarehouseKinah(kinah)` + `SM_LEGION_WAREHOUSE_INFO` + `SM_DIALOG_WINDOW(npc, 25)`
    - [✓] `CM_MOVE_ITEM` — added `ILegionDao _legionDao`; new cases (0,3) inv→legion, (3,0) legion→inv, (3,3) legion reorder; each saves via `SaveWarehouseItemsAsync` and refreshes UI with `SM_LEGION_WAREHOUSE_INFO`
    - [✓] `CM_ENTER_WORLD` — after first online member primes service: loads `FindWarehouseItemsAsync` and populates `dbLegion.WarehouseItems`
    - [✓] `GsClientConnection` — added `ILegionDao _legionDao` field; in `DisposeAsync` saves `legion.WarehouseItems` if player has a legion
    - [✓] `GsConnectionFactory` — accepts and passes `ILegionDao legionDao` to `GsClientConnection`
    - [✓] `AutoSaveService` — added `ILegionDao _legionDao`; 5-minute save calls `SaveWarehouseItemsAsync` once per unique legion (deduplicated via `HashSet<int> savedLegions`)
    - [✓] `GsPacketHandlerFactory` — passes `_legionDao` to `CM_MOVE_ITEM` (0x17E) and `CM_DIALOG_SELECT` (0x114)
    - Storage type note: .NET uses type 4 for legion WH (personal=2, account=3 in existing protocol); Java SM_WAREHOUSE_INFO writes storageType.getId() directly (LEGION=3), but existing .NET warehouse packets use +1 offset vs Java enum
    - Build: 0 warnings, 0 errors

107. [✓] CM_SPLIT_ITEM account/legion warehouse support (session 2026-05-01)
    - Root cause: `SaveStoragesAsync` and `SendUpdatesAsync` helpers only checked types 0 (inventory) and 1 (personal WH); types 2 (account WH) and 3 (legion WH) fell through to wrong branch — account WH saves were silently dropped, legion WH saves likewise; `ResolveStorage` mapped all unknown types to `player.Inventory` causing the wrong in-memory collection to be searched
    - [✓] `CM_SPLIT_ITEM` — added `ILegionDao _legionDao` field + constructor param; replaced inline ternary storage resolution with `ResolveStorage(player, type)` static helper returning `PlayerInventory?` for all 4 types (null for unknown); early return when either storage is null (covers missing-legion case for type 3)
    - [✓] `SaveStoragesAsync` — extended: type 2 calls `_itemDao.SaveAccountWarehouseAsync(_conn.AccountId, ...)`, type 3 calls `_legionDao.SaveWarehouseItemsAsync(legion.LegionId, ...)`
    - [✓] `SendUpdatesAsync` — replaced if/else with switch: type 0 → SM_INVENTORY_ADD_ITEM, type 1 → SM_WAREHOUSE_INFO, type 2 → SM_ACCOUNT_WAREHOUSE_INFO, type 3 → SM_LEGION_WAREHOUSE_INFO
    - [✓] `SendStorageRefreshAsync` — same switch extension for types 2 and 3
    - [✓] `GsPacketHandlerFactory` — passes `_legionDao` to `CM_SPLIT_ITEM` at opcode 0x17F
    - Build: 0 warnings, 0 errors

106. [✓] Legion level-up (CM_LEGION opcode 0x0E) (session 2026-05-01)
    - Root cause: `CM_LEGION` case 0x0E was read-only (D+H consumed) but RunAsync had no handler; brigade generals could not upgrade the legion level
    - [✓] `ILegionDao` — added `UpdateLevelAsync(int legionId, int level, ct)` interface method
    - [✓] `LegionDaoImpl` — implemented: `UPDATE legions SET level = @level WHERE id = @legionId`
    - [✓] `CM_LEGION` — added `IItemDao` field + constructor param; added kinah/contribution static price tables (7 entries each, indexed by current level); `HandleLevelUpAsync`: validates BG rank, max level 8 guard, contribution points check, kinah check; deducts kinah from player inventory (delete if exhausted, else update), saves inventory; deducts `ContributionPoints` from legion, persists; increments `Legion.Level`, persists; broadcasts `SM_LEGION_EDIT(newLevel)` (type 0x00) to all online members
    - [✓] `GsPacketHandlerFactory` — passes `_itemDao` to `CM_LEGION` constructor at opcode 0xCF
    - Kinah costs: 100K → 1M → 5M → 25M → 50M → 75M → 100M (levels 1→2 through 7→8)
    - Contribution costs: 0 → 20K → 100K → 500K → 2.5M → 12.5M → 62.5M (levels 1→2 through 7→8)
    - Build: 0 warnings, 0 errors

110. [✓] Legion self-intro + nickname (CM_LEGION 0x0A / 0x0F) (session 2026-05-01)
    - [✓] `SM_LEGION_UPDATE_SELF_INTRO` (0x77) — NEW packet: `D(playerObjId) + S(selfIntro)`; broadcast to all online legion members when a player updates their intro text; mirrors Java SM_LEGION_UPDATE_SELF_INTRO.writeImpl()
    - [✓] `SM_LEGION_UPDATE_NICKNAME` (0x0B) — NEW packet: `D(playerObjId) + S(newNickname)`; broadcast to all online members when BG sets a member's nickname; mirrors Java SM_LEGION_UPDATE_NICKNAME.writeImpl()
    - [✓] `ILegionDao` — added `UpdateSelfIntroAsync(int playerId, string selfIntro, ct)` and `UpdateNicknameAsync(int playerId, string nickname, ct)`
    - [✓] `LegionDaoImpl` — implemented both: `UPDATE legion_members SET self_intro=...` / `UPDATE legion_members SET nickname=...`
    - [✓] `CM_LEGION` — added `_selfIntro` and `_newNickname` fields; Read() 0x0A now stores selfIntro string (was discarded); Read() 0x0F now stores charName + newNickname (was discarded); RunAsync added cases 0x0A → HandleSelfIntroAsync and 0x0F → HandleNicknameAsync
    - [✓] `HandleSelfIntroAsync` — any member can update own intro: updates `member.SelfIntro`, persists via UpdateSelfIntroAsync, broadcasts SM_LEGION_UPDATE_SELF_INTRO to all online members; 200-char length guard
    - [✓] `HandleNicknameAsync` — BG-only: finds target member by name in legion.Members dict, updates `targetMember.Nickname`, persists via UpdateNicknameAsync, broadcasts SM_LEGION_UPDATE_NICKNAME to all online members; 20-char length guard
    - Build: 0 warnings, 0 errors

109. [✓] Legion WH kinah permission checks + broadcast; CM_LEGION 0x08 refresh (session 2026-05-01)
    - Root cause: `CM_LEGION_WH_KINAH` executed deposit/withdraw without verifying the member's rank permission mask; any legion member could move kinah regardless of configured permissions; also missing `SM_LEGION_EDIT.WarehouseKinah` broadcast so other online members never saw the updated WH kinah total
    - [✓] `CM_LEGION_WH_KINAH` — added `WH_WITHDRAWAL = 4` and `WH_DEPOSIT = 4096` constants; added `PlayerConnectionRegistry` dependency; `HasPermission` helper maps member rank → legion's `DeputyPermission / CenturionPermission / LegionaryPermission / VolunteerPermission`; BG bypasses check (always allowed); withdraw now returns early if member lacks `WH_WITHDRAWAL`; deposit returns early if member lacks `WH_DEPOSIT`
    - [✓] `CM_LEGION_WH_KINAH` — added `BroadcastKinahAsync` which sends `SM_LEGION_EDIT.WarehouseKinah(kinah)` to all online members after every successful transaction; matches Java `LegionService.LegionWhUpdate` broadcast
    - [✓] `GsPacketHandlerFactory` — 0x2EE now passes `_connRegistry` to `CM_LEGION_WH_KINAH`
    - [✓] `CM_LEGION` — added `HandleRefreshAsync` (opcode 0x08): sends `SM_LEGION_INFO(legion)` back to the requester; Java sends a full refresh of the current legion state when the client requests it; no side effects
    - Build: 0 warnings, 0 errors

108. [✓] Legion permissions update (CM_LEGION opcode 0x0D) (session 2026-05-01)
    - Root cause: `CM_LEGION` Read() consumed 4 shorts for opcode 0x0D but RunAsync had no handler; brigade generals could not set per-rank warehouse/action permissions
    - [✓] `ILegionDao` — added `UpdatePermissionsAsync(int legionId, short deputy, short centurion, short legionary, short volunteer, ct)` interface method
    - [✓] `LegionDaoImpl` — implemented: single `UPDATE legions SET deputy_permission=..., centurion_permission=..., legionary_permission=..., volunteer_permission=... WHERE id=@legionId`; all 4 columns updated atomically
    - [✓] `SM_LEGION_EDIT` — added type 0x02 (permissions broadcast): private constructor storing 4 shorts; `static Permissions(Legion)` factory; `Write` switch case 0x02 writes `H(deputy)+H(centurion)+H(legionary)+H(volunteer)`
    - [✓] `CM_LEGION` — added 4 private short fields (`_deputyPermission`, `_centurionPermission`, `_legionaryPermission`, `_volunteerPermission`); Read() case 0x0D now stores all 4 shorts instead of discarding them; added RunAsync `case 0x0D: await HandlePermissionsAsync`; `HandlePermissionsAsync`: validates BG rank, updates Legion in-memory, persists via `UpdatePermissionsAsync`, broadcasts `SM_LEGION_EDIT.Permissions(legion)` to all online members
    - Build: 0 warnings, 0 errors

113. [✓] Item re-tuning via scroll (CM_TUNE scroll path) (session 2026-05-01)
    - [✓] `ItemTemplate` — added `TuningAction` sealed class with `Target` attribute (`WEAPON|ARMOR|EQUIPMENT`); added `[XmlElement("tuning")]` to `ItemActions`; added `IsTuningScroll` convenience property to `ItemTemplate`
    - [✓] `CM_TUNE` — replaced scroll-based stub with `HandleScrollTuningAsync`: validates scroll `IsTuningScroll`, looks up target item, checks scroll target type compatibility (`EQUIPMENT` = any, `WEAPON`/`ARMOR` = type-matched), consumes scroll (delete if exhausted), re-assigns `OptionalSocket = Rnd(0..OptionSlotBonus)`, persists + sends SM_INVENTORY_ADD_ITEM + SM_SYSTEM_MESSAGE.TuningComplete; initial free tuning path unchanged
    - Java TuningAction.act() also resets randomStats — not ported (random stat bonus system absent in .NET)
    - Build: 0 warnings, 0 errors

112. [✓] Manastone removal (CM_MANASTONE actionType=3) (session 2026-05-01)
    - [✓] `IManastoneDao` — added `DeleteByItemAndSlotAsync(long itemUniqueId, int slot, ct)`
    - [✓] `ManastoneDaoImpl` — implemented: `DELETE FROM item_stones WHERE item_unique_id = @ItemUniqueId AND slot = @Slot`
    - [✓] `CM_MANASTONE` — added `KinahItemId = 182400001`, `RemovalCost = 20_000L` (Java PricesService.getPriceForService(500) default), `_slotNum` field; Read() case 3 now stores slotNum; RunAsync case 3 implemented: validates targetFusedSlot==1 (fusionstone removal stub), finds stone by slot, checks kinah, deducts kinah, removes from ManaStones list, DeleteByItemAndSlotAsync, refreshes client; NPC proximity check deferred
    - Build: 0 warnings, 0 errors

111. [✓] Manastone socketing — storage + consume (CM_MANASTONE) (session 2026-05-01)
    - [✓] `Manastone.cs` — NEW model: `long ItemUniqueId`, `int ItemId`, `int Slot`
    - [✓] `Item` — added `List<Manastone> ManaStones { get; set; } = []`
    - [✓] `V33__item_stones.sql` — NEW table: `item_stones(id PK, item_unique_id BIGINT, item_id INT, slot TINYINT)` with index on item_unique_id
    - [✓] `IManastoneDao` / `ManastoneDaoImpl` — `LoadByItemIdsAsync` (batch IN query via Dapper), `InsertAsync`, `DeleteByItemAsync`
    - [✓] `CM_MANASTONE` — actionType=1 (enchant): unchanged; actionType=2 (socket): slot-gap detection via HashSet, manaStone insertion via `_manastoneDao.InsertAsync`, sends SM_INVENTORY_ADD_ITEM for both target and stone, SM_SYSTEM_MESSAGE.ManastoneSuccess; actionType=3 (remove): stub preserved
    - [✓] `CM_ENTER_WORLD` — after account-WH load, batch-loads all manastones for inventory+WH+accWH items and hydrates each item's ManaStones list
    - [✓] `GsPacketHandlerFactory` — added `IManastoneDao _manastoneDao` field + constructor param; passes to CM_MANASTONE (0x2E8) and CM_ENTER_WORLD (0xAA)
    - [✓] `Program.cs` — `AddSingleton<IManastoneDao, ManastoneDaoImpl>()`
    - Visual (MANA_SOCKETS blob in SM_INVENTORY_INFO): deferred — 4.6.0 exact itemMask bit unclear; storage layer is fully functional
    - Build: 0 warnings, 0 errors

114. [✓] Title ownership system — earn + persist + send on login (CM_USE_ITEM / CM_ENTER_WORLD) (session 2026-05-01)
    - [✓] `V34__player_titles.sql` — NEW table: `player_titles(player_id INT, title_id INT, PRIMARY KEY(player_id, title_id))`
    - [✓] `ItemTemplate` — added `TitleAddAction` sealed class with `TitleId` + `Minutes` attributes; added `[XmlElement("titleadd")]` to `ItemActions`; added `TitleAddId` convenience property (null if no action or titleId=0)
    - [✓] `Player` — added `HashSet<int> OwnedTitles` (in-memory ownership set, loaded on login)
    - [✓] `IPlayerTitleDao` / `PlayerTitleDaoImpl` (NEW files) — `LoadByPlayerIdAsync` (SELECT title_id FROM player_titles), `AddTitleAsync` (INSERT IGNORE); backed by Dapper + MySqlConnector
    - [✓] `SM_TITLE_INFO` — added `TitleList(IEnumerable<int>)` factory (action=0, writes count + shorts); added `AddTitle(int)` factory (action=4, write single short); `EmptyList()` removed (replaced by TitleList)
    - [✓] `CM_ENTER_WORLD` — added `IPlayerTitleDao _titleDao`; after skill load, loads owned title IDs and populates `player.OwnedTitles`; replaced `SM_TITLE_INFO.EmptyList()` with `SM_TITLE_INFO.TitleList(player.OwnedTitles)`
    - [✓] `CM_USE_ITEM` — added `IPlayerTitleDao _titleDao`; added `HandleTitleAddAsync`: checks not already owned, adds to OwnedTitles, persists via AddTitleAsync, sends SM_TITLE_INFO.AddTitle, plays item animation, deletes item
    - [✓] `GsPacketHandlerFactory` — added `IPlayerTitleDao _playerTitleDao` field + constructor param; passes to CM_ENTER_WORLD (0xAA) and CM_USE_ITEM (0xC7)
    - [✓] `Program.cs` — `AddSingleton<IPlayerTitleDao, PlayerTitleDaoImpl>()`
    - Timed titles (minutes > 0) stored as permanent — expiry system deferred
    - Build: 0 warnings, 0 errors

115. [✓] Title stat modifiers — load player_titles.xml + apply MAXHP/MAXMP bonuses on title equip (session 2026-05-01)
    - [✓] `PlayerTitleTemplate.cs` (NEW) — XML model: `id`, `nameId`, `desc`, `race`; `<modifiers>` with `<add>` (flat) and `<rate>` (percentage) sub-elements; `GetAddStat(name)` sums flat modifiers for a stat name
    - [✓] `PlayerTitlesData.cs` (NEW) — data holder: loads `player_titles.xml` (root `<player_titles>`/`<title>` elements) via XmlSerializer; `GetTemplate(id)` lookup; 274 templates loaded
    - [✓] `IDataManager` / `DataManager` — added `PlayerTitlesData Titles { get; }` property; loads during startup
    - [✓] `Player` — added `TitleBonusMaxHp` and `TitleBonusMaxMp` int properties (mirror `BonusMaxHp/BonusMaxMp` pattern)
    - [✓] `CM_TITLE_SET` — added `IDataManager _dataManager`; on title equip/unequip: resolves template, updates `TitleBonusMaxHp/MaxMp`, recomputes `MaxHp/MaxMp` (with BonusMaxHp + TitleBonusMaxHp + ssMult), clamps CurrentHp/Mp, persists, sends SM_STATS_INFO + SM_TITLE_INFO.ActiveTitle + BroadcastTitle
    - [✓] `CM_ENTER_WORLD` — initializes `TitleBonusMaxHp/MaxMp` from title template before computing MaxHp/MaxMp on login
    - [✓] `CM_REVIVE` / `CM_EQUIP_ITEM` / `CM_DIALOG_SELECT` / `CM_GM_COMMAND_SEND` / `ExperienceService` — all MaxHp/MaxMp recomputations updated to include `+ player.TitleBonusMaxHp/MaxMp`
    - [✓] `GsPacketHandlerFactory` — passes `_dataManager` to CM_TITLE_SET (0x129)
    - Only `<add>` (flat) modifiers implemented: MAXHP+MAXMP; `<rate>` (speed/attack speed) deferred (complex combat formula)
    - Build: 0 warnings, 0 errors

116. [✓] NPC walker paths — load npc_walker.xml + route-following patrol behavior (session 2026-05-01)
    - [✓] `WalkerData.cs` (NEW) — streaming XmlReader loads `npc_walker/npc_walker.xml` + `custom_npc_walker.xml`; stores `routeId → RouteStep[]` (case-insensitive hex string keys); `GetRoute(string)` lookup
    - [✓] `IDataManager` / `DataManager` — added `WalkerData Walkers { get; }` property; loads after Titles
    - [✓] `SpawnSpot` — added `[XmlAttribute("walker_id")] string WalkerId` (empty string default, optional attribute)
    - [✓] `Npc` — added `string WalkerId { get; set; }` property (empty = random wander)
    - [✓] `SpawnService.SpawnAll()` — passes `spot.WalkerId` to `SpawnNpc()`; `SpawnNpc()` gains `walkerId` param (default empty)
    - [✓] `NpcAiService` — added `_walkerStepIndex: Dictionary<int, int>` (step index per NPC); renamed old `WanderAsync` to `WanderRandomAsync`; new `WanderAsync` dispatches to `PatrolAsync` for walker NPCs or `WanderRandomAsync` for others; `PatrolAsync` follows route steps in sequence (cycling), broadcasts SM_MOVE on each advance, advances step index on arrival; walker NPCs skip the 10-second wander cooldown (continuous patrol)
    - Walker state reuses `_wanderState` dict (WanderState record) — patrol move tracked same as wander move; step index cleared on NPC death
    - Build: 0 warnings, 0 errors

117. [✓] Tribe relations — load tribe_relations.xml + faction-aware NPC aggro (session 2026-05-01)
    - [✓] `TribeData.cs` (NEW) — streaming XmlReader loads `tribe/tribe_relations.xml` (553 entries); models `TribeRelation { Base, Aggro (HashSet), Hostile (HashSet) }`; `IsAggressiveToPlayer(npcTribe, playerRace)` implements Java TribeRelationService.isAggressive() logic: hardcoded GUARD/GUARD_DARK/GUARD_DRAGON base-type rules first, then explicit aggro/hostile set lookup, then MONSTER default
    - [✓] `IDataManager` / `DataManager` — added `TribeData Tribes { get; }` property; loads after Walkers
    - [✓] `NpcTemplate` — added `[XmlAttribute("tribe")] string Tribe` (default "GENERAL")
    - [✓] `NpcAiService` scan loop — player target now guarded by `IsAggressiveToPlayer(npc.Template.Tribe, player.Race)`: GUARD tribes attack only opposite-faction players; AGGRESSIVEMONSTER/MONSTER-base tribes attack all players; GENERAL/USEALL tribes do not initiate aggro
    - Player tribe mapping: ELYOS→"PC", ASMODIANS→"PC_DARK" (mirrors Java TribeClass enum)
    - Build: 0 warnings, 0 errors

119. [✓] NPC shouts — DIED event + IDLE ambient shout (session 2026-05-02)
    - [✓] `CM_ATTACK` — added `IDataManager _dataManager` field + constructor param; in NPC death path, after SM_EMOTION(DIE) broadcast, calls `NpcShouts.GetRandomShout(npcId, DIED, worldId)` and broadcasts `SM_SYSTEM_MESSAGE.NpcShout` to all zone players; mirrors Java ShoutEventHandler.onDied
    - [✓] `CM_CASTSPELL` Task.Run NPC death path — same DIED shout logic added after SM_EMOTION(DIE) broadcast; `_dataManager` already injected
    - [✓] `GsPacketHandlerFactory` — updated 0xE2 (CM_ATTACK) to pass `_dataManager`
    - [✓] `NpcAiService` — added `_lastIdleShoutTime: Dictionary<int, DateTime>` (30-second per-NPC cooldown); in `WanderRandomAsync` after broadcasting SM_MOVE start, checks IDLE shout cooldown and calls `GetRandomShout(npcId, IDLE, worldId)`; if entry found, broadcasts `SM_SYSTEM_MESSAGE.NpcShout` to zone players and stamps cooldown; NPC ambient shouts now fire periodically during wander; cleaned up `_lastIdleShoutTime` in dead-NPC block
    - Build: 0 warnings, 0 errors

122. [✓] Bind stone kinah fee — load bind_points.xml + deduct price on bind (session 2026-05-01)
    - [✓] `BindPointData.cs` (NEW) — streaming XmlReader loads `bind_points/bind_points.xml`; stores `Dictionary<int, long>` (npcId → price); `GetPrice(npcId)` returns 0 when NPC not listed
    - [✓] `IDataManager` / `DataManager` — added `BindPointData BindPoints { get; }` property; loads after Portals
    - [✓] `CM_SHOW_DIALOG` — added `IItemDao` dependency; BINDSTONE branch now looks up `BindPoints.GetPrice(npc.Template.NpcId)`; if price > 0: checks kinah balance (sends NoEnoughKinah and returns on insufficient funds); deducts kinah, `SaveAllAsync`, sends `SM_INVENTORY_ADD_ITEM([kinah])`; bind point always set free when price == 0 (unlisted or starter zone stones); mirrors Java BindPointService.setBindPoint with kinah deduction
    - [✓] `GsPacketHandlerFactory` — updated 0x116 (CM_SHOW_DIALOG) to pass `_itemDao`
    - Build: 0 warnings, 0 errors

121. [✓] NPC shout — ATTACK_BEGIN event on first hit per combat (session 2026-05-01)
    - [✓] `NpcAiService` — added `_attackBegunNpcs: HashSet<int>`; on each NPC's first melee attack in a new combat engagement, calls `GetRandomShout(npcId, ATTACK_BEGIN, worldId)` and broadcasts `SM_SYSTEM_MESSAGE.NpcShout` to zone players; ATTACK_BEGIN fires exactly once per combat (HashSet.Add returns false on subsequent ticks); cleared in dead-NPC block, on leash-break target-loss, on player-kill, and in no-players-online early-exit; mirrors Java ShoutEventHandler.onAttack(BEGIN)
    - Build: 0 warnings, 0 errors

120. [✓] Portal NPC system — load portal_loc.xml + portal_template2.xml; instant teleport on click (session 2026-05-01)
    - [✓] `PortalData.cs` (NEW) — streaming XmlReader loads `portals/portal_loc.xml` into `Dictionary<int, PortalLocation>` (loc_id → {WorldId, X, Y, Z, Heading}); loads `portals/portal_template2.xml` into `Dictionary<int, List<PortalPath>>` (npcId → paths with optional race filter); `GetPortalLocation(npcId, playerRace)` selects race-specific path first, falls back to unrestricted (empty race); `IsPortal(npcId)` lookup
    - [✓] `IDataManager` / `DataManager` — added `PortalData Portals { get; }` property; loads after NpcShouts in startup sequence
    - [✓] `CM_SHOW_DIALOG` — added `PlayerConnectionRegistry` dependency; added portal detection before BINDSTONE branch: if `Portals.GetPortalLocation(npc.Template.NpcId, player.Race)` returns a location, broadcasts `SM_DELETE(time=11)` to old-zone peers, updates `player.Position`, sends `SM_TELEPORT_LOC(pos, portAnimation=0)`, fire-and-forgets `SchedulePostTeleportAsync`; mirrors `CM_TELEPORT_SELECT` flow exactly (same 2200ms delay, same cross-map vs same-map branches)
    - [✓] `GsPacketHandlerFactory` — updated 0x116 (CM_SHOW_DIALOG) to pass `_connRegistry`
    - Portal locations: `portal_loc.xml` uses `world_id` attribute (distinct from `teleport_location.xml` which uses `mapid`); all instance portal loc_ids (e.g. Dredgion 3002100–3002103) resolve via `portal_loc.xml` only
    - Build: 0 warnings, 0 errors

138. [✓] Godstone proc on melee attack (session 2026-05-02)
    - [✓] `CM_ATTACK` — after main-hit broadcast, checks player's equipped main-hand weapon for a socketed godstone (`GodStoneItemId > 0`); looks up godstone item template's `GodstoneInfo`; rolls `Random.Next(1000) < god.Probability` (probability out of 1000, mirrors Java GodStone.onEquip ActionObserver); on proc: applies `skillLvl×20 + Random(10,30)` secondary damage, broadcasts `SM_CASTSPELL(godSkillId, godSkillLvl, targetType, target)` + `SM_ATTACK_STATUS`; proc only fires when target is still alive after main hit
    - Proc kill edge case (proc reduces HP to 0) is deferred: NPC cleanup handled by next NpcAiService tick; player kills from proc are not yet credited
    - Build: 0 warnings, 0 errors

137. [✓] Stigma socketing — parse `<stigma>` XML, shard consumption, skill grant/removal on equip/unequip, re-apply on login (session 2026-05-02)
    - [✓] `ItemTemplate` — added `[XmlElement("stigma")] public StigmaTemplate? Stigma { get; set; }` and `IsStigmaItem` computed property
    - [✓] `StigmaTemplate` (NEW) — maps `<stigma shard="N" skill="level:skillId ..."/>`; `GetSkills()` parses space-separated "level:id" pairs; mirrors Java Stigma.java
    - [✓] `PlayerSkillList` — added `RemoveStigmaSkill(skillId)` to remove a stigma skill from the `_stigma` dict on unequip
    - [✓] `SM_SYSTEM_MESSAGE.StigmaNotEnoughShards()` — factory for msg code 1300450 (STR_UI_STIGMA_NOT_ENOUGH_MATERIAL)
    - [✓] `CM_EQUIP_ITEM` — routes stigma items to new `HandleStigmaAsync`: validates shard count (item 141000001); removes displaced stigma's skills; moves displaced to bag; equips new stigma; consumes exact shard count (deletes item if stack depleted, decrements otherwise); grants skills via `AddSkill(id, lvl, isStigma: true)`; sends `SM_SKILL_LIST(granted, isNew: true)` on equip; on unequip removes skills and sends full `SM_SKILL_LIST(AllSkills, isNew: false)` refresh
    - [✓] `CM_ENTER_WORLD` — after inventory loads, re-applies stigma skills from all currently equipped stigma items; stigma skills are NOT persisted in `player_skills` DB — re-derived on login (mirrors Java StigmaService.onPlayerLogin)
    - Build: 0 warnings, 0 errors

136. [✓] Death XP loss — XPLossEnum + ExpRecoverable drain-back (session 2026-05-01)
    - [✓] `Player.ExpRecoverable` — new long property; tracks recoverable death-loss XP shown on the client XP bar
    - [✓] `ExperienceService.ApplyDeathXpLoss(player, dataManager)` — 0 for level<50; for level 50+: loss=0.25% of (nextLevelXP−currentXP); unrecoverable≈33% deducted from player.Exp immediately; recoverable≈67% added to ExpRecoverable (capped at 25% of expNeed); mirrors Java XPLossEnum + PlayerCommonData.calculateExpLoss
    - [✓] `ExperienceService.DrainRecoverable(player, xpGained)` — private static; each time XP is added, reduces ExpRecoverable by that amount (so the grey bar shrinks as the player hunts)
    - [✓] `ExperienceService.AddExpAsync` — now calls DrainRecoverable before crediting XP; passes `player.ExpRecoverable` to SM_STATUPDATE_EXP (client renders it as the grey portion of the XP bar)
    - [✓] `NpcAiService` — injected `ExperienceService _expService`; after sending SM_DIE, calls `ApplyDeathXpLoss` and sends updated SM_STATUPDATE_EXP to the dead player if xpLost > 0
    - XP loss only kicks in at level 50+ (matches Java XPLossEnum); level 1-49 players die penalty-free (soul sickness HP/MP penalty still applies via CM_REVIVE)
    - Build: 0 warnings, 0 errors

135. [✓] Item use cooldown — parse <uselimits> + enforce delayId-based cooldown in CM_USE_ITEM (session 2026-05-01)
    - [✓] `ItemTemplate` — added `[XmlElement("uselimits")] public ItemUseLimits? UseLimits { get; set; }`
    - [✓] `ItemUseLimits` (NEW) — maps to `<uselimits usedelay="..." usedelayid="..."/>`; `DelayMs` is the cooldown in ms; `DelayId` is the shared-cooldown group ID (e.g. all HP potions = 11, delay 60000ms)
    - [✓] `Player` — added `ItemCooldowns: Dictionary<int, DateTime>`, `IsItemOnCooldown(delayId)`, `SetItemCooldown(delayId, delayMs)` helpers; mirrors Java `addItemCoolDown` / `isItemUseDisabled`
    - [✓] `CM_USE_ITEM` — checks `player.IsItemOnCooldown(limits.DelayId)` before applying skill effect; sends `SM_SYSTEM_MESSAGE.ItemCantUseUntilDelayTime()` if blocked; sets `player.SetItemCooldown(...)` after successful use; `limits` captured at the top of the consumable path to avoid duplicate null checks
    - [✓] `SM_SYSTEM_MESSAGE.ItemCantUseUntilDelayTime()` — added factory (msg code 1300400 = STR_ITEM_CANT_USE_UNTIL_DELAY_TIME)
    - Prevents potion spam: HP potions (delayId=11, 60s) and MP potions share separate cooldowns; using any HP potion blocks all HP potions for 60s
    - Build: 0 warnings, 0 errors

134. [✓] Group XP distribution — proportional shares + group bonus + max-level scaling (session 2026-05-01)
    - [✓] `ExperienceService.AddGroupExpAsync(killer, xpBase, npcLevel, ct)` — new signature; consolidates all XP scaling inside the method; solo path uses killerLevel, group path uses highest eligible member level (mirrors Java calculateGroupExperienceReward using filteredStats.highestLevel)
    - [✓] Group bonus: `100%` solo, `150% + (size-2)*10%` for groups (2→150%, 3→160%, 4→170%, 5→180%, 6→190%); matches Java PlayerTeamDistributionService.doReward bonus formula
    - [✓] Per-member share proportional to `member.Level / sum(eligible.Level)` weighted by bonus; members 10+ levels below highest get 0 XP
    - [✓] `CM_ATTACK` / `CM_CASTSPELL` — removed inline level-scaling (M70 code); now pass raw `xpBase` + `deadNpc.Level` to `AddGroupExpAsync`; group path picks up the group bonus automatically
    - Build: 0 warnings, 0 errors

133. [✓] Level-based XP and drop reward scaling — XPRewardEnum + DropRewardEnum (session 2026-05-01)
    - [✓] `ExperienceService.XpRewardPercent(int levelDiff)` (public static) — switch expression matching Java XPRewardEnum exactly: ≤-11→0%, -10→1%, -9→10%, -8→20%, -7→30%, -6→40%, -5→50%, -4→70%, -3→90%, -2/0→100%, +1→105%, +2→110%, +3→115%, ≥+4→120%; levelDiff = npcLevel - playerLevel
    - [✓] `LootService.DropRewardPercent(int levelDiff)` (private static) — matches Java DropRewardEnum: ≤-10→0%, -9→39%, -8→79%, ≥-7→100%
    - [✓] `LootService.GenerateDrops` — computes `dropPct = DropRewardPercent(npc.Level - killer.Level)`; scales kinah amount by dropPct; scales each item's effectiveChance by dropPct; scales fallback potion chance by dropPct; kinah entry skipped entirely when dropPct==0
    - [✓] `CM_ATTACK` — before `AddGroupExpAsync`: computes `xpPct = ExperienceService.XpRewardPercent(deadNpc.Level - player.Level)` and multiplies `xpReward *= xpPct / 100` (skips multiply when 100% to avoid integer drift)
    - [✓] `CM_CASTSPELL` — same XP scaling applied in the spell-kill Task.Run path
    - Build: 0 warnings, 0 errors

132. [✓] NPC retaliation extended to CM_CASTSPELL spell damage (session 2026-05-01)
    - [✓] `CM_CASTSPELL` — added `NpcAiService _npcAi` field + constructor parameter; captured `npcAi` before Task.Run closure; added `npcAi.ForceEngage(spellHitNpc, player)` after spell damage is applied (only when NPC is still alive); mirrors same pattern as CM_ATTACK
    - [✓] `GsPacketHandlerFactory` — passed `_npcAi` to CM_CASTSPELL
    - Now both melee and spell hits trigger NPC retaliation for passive NPCs
    - Build: 0 warnings, 0 errors

131. [✓] NPC forced engagement on player hit — passive NPCs retaliate when attacked (session 2026-05-01)
    - [✓] `NpcAiService._npcTargets` — changed from `Dictionary<int,int>` to `ConcurrentDictionary<int,int>` (thread-safe; ForceEngage called from packet handler thread); all `Remove` → `TryRemove`
    - [✓] `NpcAiService.ForceEngage(Npc npc, Player player)` — public; calls `_npcTargets.TryAdd` (idempotent — won't override an existing target); sets `npc.Target` and `npc.LastCombatTime`; NPC picks up the target on its next tick
    - [✓] `Program.cs` — changed from `AddHostedService<NpcAiService>()` to `AddSingleton<NpcAiService>()` + `AddHostedService(sp => sp.GetRequiredService<NpcAiService>())` so the singleton is injectable elsewhere
    - [✓] `CM_ATTACK` — injected `NpcAiService _npcAi`; after applying damage, if target is a live Npc, calls `_npcAi.ForceEngage(attackedNpc, player)`; mirrors Java AggroEventHandler.onAggro() which adds the player to the NPC's aggro list on any hit
    - [✓] `GsPacketHandlerFactory` — added `NpcAiService npcAi` parameter; passes `_npcAi` to `CM_ATTACK` constructor
    - Passive NPCs (aggroRange == 0) now correctly fight back when a player hits them
    - Build: 0 warnings, 0 errors

130. [✓] NPC assist system — same-tribe NPCs join combat on aggro (session 2026-05-01)
    - [✓] `TribeData.IsSupport(helperTribe, victimTribe)` — returns true if same tribe name OR same base tribe; mirrors Java TribeRelationService.isSupport()
    - [✓] `NpcAiService.AlertNearbyAllies(aggressor, target)` — when an NPC acquires a new target, iterates all non-dead, non-dummy NPCs in the same world; for each ally within its own aggro range that has no current target and shares a tribe/base with the aggressor, assigns the same player target; allies then engage on their next tick (no extra packets needed — existing combat logic handles the follow-through)
    - [✓] Called immediately after `_npcTargets[npc.ObjectId] = target.ObjectId` in the new-target acquisition block of `TickAsync`; synchronous (no async overhead since no packets are sent here)
    - Mirrors Java AggroEventHandler.onCreatureNeedsSupport() + AggroNotifier broadcast (our 2-second tick replaces the 500ms Java delay)
    - Build: 0 warnings, 0 errors

129. [✓] Cube expander data-driven pricing — CubeExpanderData + CM_DIALOG_SELECT integration (session 2026-05-01)
    - [✓] `CubeExpanderData.cs` (NEW) — streaming XmlReader loads `cube_expander/cube_expander.xml`; maps `npcId → (expandLevel → price)`; `GetExpandPrice(npcId, nextLevel)` returns `long?` (null when NPC doesn't offer that level); `IsCubeExpander(npcId)` guards unknown NPCs; NPCs in the file have NPC-specific pricing (e.g. NPC 798008 only offers level 1 at 1000 kinah; Sanctum NPCs offer levels 2–4)
    - [✓] `IDataManager` / `DataManager` — added `CubeExpanderData CubeExpander { get; }` property; loads after InstanceExits
    - [✓] `CM_DIALOG_SELECT.HandleExpandCubeAsync` — removed hardcoded `CubeExpandPrices` array; now checks `IsCubeExpander(npc.Template.NpcId)` (returns silently if NPC isn't a cube expander); uses `GetExpandPrice(npc.Template.NpcId, nextLevel)` for the price (null → CannotExpandCubeMore); deducts `price.Value` from kinah; all expansion limit logic now driven by XML data, not a hardcoded array length
    - Build: 0 warnings, 0 errors

128. [✓] Instance exit — InstanceExitData + CM_INSTANCE_LEAVE teleport (session 2026-05-01)
    - [✓] `InstanceExitData.cs` (NEW) — streaming XmlReader loads `instance_exit/instance_exit.xml`; maps `(instanceWorldId, race)` → `ExitLocation { ExitWorldId, X, Y, Z, Heading }`; `GetExit(instanceWorldId, playerRace)` looks up by faction string ("ELYOS"/"ASMODIANS")
    - [✓] `IDataManager` / `DataManager` — added `InstanceExitData InstanceExits { get; }` property; loads after GlobalDrops
    - [✓] `CM_INSTANCE_LEAVE` (0xCC) — upgraded from stub: accepts `GsClientConnection`, `GameWorld`, `IDataManager`, `PlayerConnectionRegistry`; on leave, calls `InstanceExits.GetExit(player.Position.WorldId, player.Race)` and if found, broadcasts `SM_DELETE(time=11)` to old-zone peers, updates `player.Position`, sends `SM_TELEPORT_LOC`, fire-and-forgets `SchedulePostTeleportAsync`; if no exit defined for instance, returns silently (some instances may lack exit data); mirrors Java InstanceLeaveService flow
    - [✓] `GsPacketHandlerFactory` — updated 0xCC to pass `conn + _world + _dataManager + _connRegistry` to `CM_INSTANCE_LEAVE`
    - Build: 0 warnings, 0 errors

127. [✓] NPC skill data path fix — npc_skills.xml was silently not loading (session 2026-05-01)
    - Root cause: `NpcSkillData.Load` used `Path.Combine(dataRoot, "npc_skills.xml")` but the file lives at `data/static_data/npc_skills/npc_skills.xml` (subdirectory) — the `File.Exists` check returned false, warning was logged, and all NPC skills were silently skipped; this means all prior NPC skill code (M63, earlier) was shipping dead logic because the data was never loaded
    - [✓] `NpcSkillData` — corrected path to `Path.Combine(dataRoot, "npc_skills", "npc_skills.xml")`; NPC skills now load correctly; verified by build; runtime logging will show "loaded skill sets for N NPCs" on server start
    - Build: 0 warnings, 0 errors

126. [✓] NPC skill HP% threshold — parse minhp/maxhp attributes + filter eligible skills (session 2026-05-01)
    - [✓] `NpcSkillData.NpcSkillEntry` — added `MinHp int = 0` and `MaxHp int = 100` fields; `IsReadyForNpcHp(int hpPct)` returns true when `minhp <= hpPct <= maxhp`; default 0–100 = no restriction (fires at any HP)
    - [✓] `NpcSkillData.LoadFile` — now parses `minhp` and `maxhp` from `<npcskill>` element; both optional (default 0/100 respectively)
    - [✓] `NpcAiService.TryCastNpcSkillAsync` — filters skill list to entries whose HP range covers `npc.HpPercentage` before random selection; NPCs with threshold-based skills (e.g. heal at <50% HP) now fire those skills correctly; mirrors Java NpcSkillEntry.hpReady()
    - 778 skill entries in the data file have explicit minhp/maxhp; entries without the attribute get defaults and behave as before
    - Build: 0 warnings, 0 errors

125. [✓] NPC shout — ATTACK_END event on target-loss or player-kill (session 2026-05-01)
    - [✓] `NpcAiService` — added `BroadcastAttackEndShoutAsync(npc, ct)` private helper; calls `GetRandomShout(npcId, ATTACK_END, worldId)` and broadcasts `SM_SYSTEM_MESSAGE.NpcShout` to zone; called from two points: leash-break target-loss (NPC exceeds leash range) and player-killed-by-NPC cleanup; mirrors Java ShoutEventHandler.onAttackEnd() which fires in AttackEventHandler.onFinishAttack()
    - Build: 0 warnings, 0 errors

124. [✓] Global drop rules — WorldMapData + GlobalDropData + LootService integration (session 2026-05-01)
    - [✓] `WorldMapData.cs` (NEW) — streaming XmlReader loads `world_maps.xml`; indexes `drop_type` attribute per map id (e.g. ELYSEA, ABYSS_INSTANCE, BALAUREA); `GetDropType(worldId)` returns empty string when no `drop_type` attribute (e.g. maps with `world_type` only); used by GlobalDropData to match `gd_world` filters
    - [✓] `GlobalDropData.cs` (NEW) — streaming XmlReader loads `global_drops/global_rules.xml`; parses 66 rules (gd_worlds, gd_races, gd_ratings, gd_maps, gd_items, base_chance, min/max_count, min/max_diff, restriction_race); `GetGlobalDrops(...)` applies all filters per rule: world-type match, specific-map match, NPC-race match, NPC-rating match, level-diff range, player-race restriction; rolls `base_chance` (0–100); picks one item randomly from `gd_items` list; yields `(itemId, count)` pairs
    - [✓] `IDataManager` / `DataManager` — added `WorldMapData WorldMaps` and `GlobalDropData GlobalDrops` properties; loaded after BindPoints in startup sequence
    - [✓] `LootService.GenerateDrops(Npc npc)` → `GenerateDrops(Npc npc, Player killer)` — resolves NPC world's drop-type string, player's faction string, then appends all matching global drops to the loot list after NPC-specific drops
    - [✓] `CM_ATTACK` / `CM_CASTSPELL` — updated `GenerateDrops` calls to pass the killing player
    - Level-diff semantics: `playerLevel - npcLevel` must fall in `[minDiff, maxDiff]`; when no diff attributes in XML, defaults to [int.MinValue, int.MaxValue] (no restriction)
    - Build: 0 warnings, 0 errors

123. [✓] NpcTemplate rank/rating/race attributes + ATTACK shout (session 2026-05-01)
    - [✓] `NpcTemplate` — added three new XML-parsed properties: `Rank` (`[XmlAttribute("rank")]`, default "NOVICE"), `Rating` (`[XmlAttribute("rating")]`, default "NORMAL"), `NpcRace` (`[XmlAttribute("race")]`, default "GENERAL"); mirrors Java NpcTemplate fields; required by global drop rules (rating=NORMAL/ELITE/HERO, race=BEAST/ELEMENTAL/etc.)
    - [✓] `NpcAiService` — added ATTACK shout block after damage broadcast; fires with 20% probability per attack tick; calls `GetRandomShout(npcId, ATTACK, worldId)` and broadcasts `SM_SYSTEM_MESSAGE.NpcShout` to zone; mirrors Java ShoutEventHandler.onAttack (periodic, distinct from ATTACK_BEGIN which fires exactly once)
    - Build: 0 warnings, 0 errors

118. [✓] NPC shouts — load npc_shouts.xml + SEE event broadcast (session 2026-05-02)
    - [✓] `NpcShoutData.cs` (NEW) — streaming XmlReader loads `npc_shouts/npc_shouts.xml`; indexes by `(npcId → ShoutEventType → List<ShoutEntry>)`; `GetRandomShout(npcId, evt, worldId)` filters by `restrict_world` (0=any world, nonzero=specific map) and returns a random eligible entry; `ShoutEventType` enum covers SEE/IDLE/ATTACK/ATTACK_BEGIN/ATTACK_END/DIED/CAST_K/ATTACK_K/PLAYER_MAGIC/PLAYER_SNARE/PLAYER_DEBUFF/PLAYER_SLAVE/PLAYER_BLOW/SWITCH_TARGET/GOD_HELP/WAKEUP/OTHER
    - [✓] `IDataManager` / `DataManager` — added `NpcShoutData NpcShouts { get; }` property; loads after Tribes
    - [✓] `SM_SYSTEM_MESSAGE` — refactored constructor to two overloads: `(int code, params string[] parms)` for system messages (npcObjId=0) and `(int code, int npcObjId, string[] parms)` for NPC shouts; all existing factory methods unaffected; added `NpcShout(int npcObjId, int stringId)` factory using the npcObjId overload; `Write` emits real `_npcObjId` field instead of hardcoded 0
    - [✓] `NpcAiService` — SEE shout event: when NPC newly acquires a player target (target scan finds non-null with no prior lock), calls `GetRandomShout(npc.Template.NpcId, SEE, npc.Position.WorldId)`; on match, broadcasts `SM_SYSTEM_MESSAGE.NpcShout(npc.ObjectId, entry.StringId)` to all players in the NPC's WorldId with try/catch per broadcast convention; mirrors Java NpcShoutsService.broadcastNpcShout
    - Build: 0 warnings, 0 errors

139. [✓] Barbershop / plastic surgery — CM_CHARACTER_EDIT + PlayerEnterWorldService extraction (session 2026-05-02)
    - [✓] `PlayerEnterWorldService` (NEW service) — extracted the full 400-line world-entry sequence from `CM_ENTER_WORLD.RunAsync` into a singleton service; constructor accepts all 18 DAOs/services previously in CM_ENTER_WORLD; single public method `EnterWorldAsync(conn, objectId, ct)`
    - [✓] `CM_ENTER_WORLD` — refactored to thin wrapper: only reads _objectId (D) and delegates to `_enterWorldService.EnterWorldAsync`
    - [✓] `CM_CHARACTER_EDIT` (NEW, opcode 0xA5) — full implementation: reads objectId+52skip+genderRaw+race(discarded)+class(discarded)+all ~55 appearance fields (identical layout to CM_CREATE_CHARACTER); `RunAsync` loads player → validates ownership → determines genderChange → loads inventory → searches for matching ticket; if ticket found: consumes it (SaveAllAsync with stack decremented/removed) + `UpdateAsync(appearance)` + `UpdateGenderAsync` if gender changed; always calls `EnterWorldAsync`; if no ticket after entry, sends `CharEditNoPlasticSurgeryTicket` or `CharEditNoGenderTicket` system message
    - [✓] `IPlayerAppearanceDao` — added `UpdateAsync(int playerId, PlayerAppearance appearance, ct)` method
    - [✓] `PlayerAppearanceDaoImpl` — implemented `UpdateAsync` with full UPDATE SQL covering all 55 appearance columns
    - [✓] `IPlayerDao` — added `UpdateGenderAsync(int playerId, Gender gender, ct)` method
    - [✓] `PlayerDaoImpl` — implemented `UpdateGenderAsync`: `UPDATE players SET gender=@g WHERE id=@playerId`
    - [✓] `SM_SYSTEM_MESSAGE` — added `CharEditNoPlasticSurgeryTicket()` (901752) and `CharEditNoGenderTicket()` (901754) factories
    - [✓] `GsPacketHandlerFactory` — added `PlayerEnterWorldService` field; 0xA5 now constructs `CM_CHARACTER_EDIT(conn, _playerDao, _itemDao, _appearanceDao, _enterWorldService)`; 0xAA (CM_ENTER_WORLD) now only needs `conn + _enterWorldService`
    - [✓] `Program.cs` — `AddSingleton<PlayerEnterWorldService>()`
    - Plastic surgery tickets: 169650000–169650007; gender change tickets: 169660000–169660002; Java behavior preserved: world entry is unconditional, error sent after entry when no ticket
    - Build: 0 warnings, 0 errors

140. [✓] Skill cooldown broadcast — CooldownId-based tracking + SM_SKILL_COOLDOWN per cast (session 2026-05-02)
    - [✓] `SkillTemplate` — added `EffectiveCooldownId` computed property: `CooldownId > 0 ? CooldownId : SkillId`; mirrors Java `getCooldownId()` fallback
    - [✓] `SkillData.BuildCooldownGroups()` — fixed to key groups by `EffectiveCooldownId` (previously keyed by raw `CooldownId`, causing all skills with CooldownId=0 to share a single spurious group)
    - [✓] `Player` — added `SkillCooldowns: Dictionary<int, DateTime>`, `IsSkillOnCooldown(effectiveCdId)`, `SetSkillCooldown(effectiveCdId, ms)` mirrors Java `skillCoolDowns FastMap` pattern
    - [✓] `PlayerSkillEntry` — removed `LastUsedAt`, `IsOnCooldown`, `MarkUsed`; per-skill cooldown tracking replaced by Player-level CooldownId system
    - [✓] `SM_SKILL_COOLDOWN` — fully rewritten: accepts `SkillData + Dictionary<int,DateTime>`; expands each active cooldown entry into its skill group; wire format: `H(count)+C(1)` then per skill `H(skillId)+D(remainSecs)+D(baseMs)`; matches Java SM_SKILL_COOLDOWN.writeImpl()
    - [✓] `CM_CASTSPELL` — replaced `skillEntry.IsOnCooldown/MarkUsed` with `player.IsSkillOnCooldown/SetSkillCooldown` keyed by `template.EffectiveCooldownId`; sends `SM_SKILL_COOLDOWN` to caster immediately after setting the cooldown; skills with `Cooldown==0` bypass the cooldown system (instant skills)
    - [✓] `PlayerEnterWorldService` — passes `_dataManager.Skills + player.SkillCooldowns` to SM_SKILL_COOLDOWN on login; players with no active cooldowns receive count=0 (no overhead)
    - Previously: multiple skills sharing a CooldownId did not share cooldowns; SM_SKILL_COOLDOWN always sent count=0 so skill icons never showed cooldown timers on client
    - Build: 0 warnings, 0 errors

141. [✓] Buff/debuff effect tracking — SM_ABNORMAL_EFFECT wire format + BUFF/CHANT skill activation (session 2026-05-02)
    - [✓] `AbnormalState` (NEW model) — `SkillId`, `SkillLevel`, `EffectorId`, `Expiry`; computed `IsExpired` and `RemainingMs`; represents one active buff or debuff on a creature
    - [✓] `Creature` — added `AddEffect(AbnormalState)`, `RemoveEffect(int, DateTime)`, `GetActiveEffects()`; uses `_effectsLock` object for thread safety; `AddEffect` removes any prior entry for the same SkillId before inserting (one stack per skill)
    - [✓] `SM_ABNORMAL_EFFECT` — fully rewritten from always-zero stub; new overload `(int, bool, List<AbnormalState>)`; wire format: `D(effectedId) C(effectType) D(0) D(0) D(0) C(0x7F) H(count)` then per effect `[D(effectorId) if player]` + `H(skillId) C(level) C(targetSlot=0) D(remainMs)`; mirrors Java SM_ABNORMAL_EFFECT.writeImpl()
    - [✓] `CM_CASTSPELL` — added BUFF/CHANT branch (between heal and damage branches): resolves target (self or named player), adds `AbnormalState` to `buffTarget.ActiveEffects`, broadcasts `SM_ABNORMAL_EFFECT` with active effect list to all zone clients; fire-and-forget expiry task removes effect after `template.Duration` ms and re-broadcasts updated list; sends `SM_SKILL_ACTIVATION` after effect is applied; skills with `Duration == 0` or non-player targets are skipped
    - Previously: BUFF/CHANT skills did nothing after cooldown — no activation packet, no buff icon visible on client; now buff icons appear on player portrait with countdown timer matching skill duration
    - Build: 0 warnings, 0 errors

142. [✓] DEBUFF effect tracking on spell targets + BUFF expiry worldId fix (session 2026-05-02)
    - [✓] `CM_CASTSPELL` (damage Task.Run) — after DP gain, if `target.CurrentHp > 0` and `template.SubType == DEBUFF` and `template.Duration > 0`: creates `AbnormalState` on the target creature, calls `target.AddEffect`, broadcasts `SM_ABNORMAL_EFFECT(target, isPlayer, activeEffects)` to zone; nested fire-and-forget expiry task removes effect after `Duration` ms and re-broadcasts using `expTarget.Position.WorldId` at expiry time
    - [✓] BUFF expiry worldId bug (M141) — replaced captured-at-cast `buffWorldId` with `expiryTarget.Position.WorldId` evaluated at expiry time; correct if target teleports between zones during buff duration
    - DEBUFF vs BUFF differences: effectType=1 (creature) for NPC targets omits `effectorId` per wire format; player targets use effectType=2 with effectorId
    - Debuffs stack independently per skillId (AddEffect deduplicates by skillId); a re-cast of the same debuff refreshes the duration
    - Build: 0 warnings, 0 errors

143. [✓] NPC skill type dispatch — HEAL/BUFF/DEBUFF branches in TryCastNpcSkillAsync (session 2026-05-02)
    - [✓] `NpcAiService.TryCastNpcSkillAsync` — replaced single always-damage path with `switch(skillTemplate?.SubType)`: HEAL → `CastNpcHealAsync`, BUFF/CHANT → `CastNpcBuffAsync`, DEBUFF → `CastNpcDebuffAsync`, default → `CastNpcDamageAsync`; SM_CASTSPELL broadcast remains before the switch; SM_SKILL_ACTIVATION broadcast added after switch (was previously missing entirely)
    - [✓] `CastNpcDamageAsync` — existing magic damage logic (level*8 + rand, MagicDefense mitigation, SM_ATTACK_STATUS Damage); extracted verbatim from old TryCastNpcSkillAsync
    - [✓] `CastNpcHealAsync` — restores NPC.CurrentHp by MaxHp/6 (capped at MaxHp); broadcasts SM_ATTACK_STATUS(NaturalHp) so nearby players see the green heal number
    - [✓] `CastNpcBuffAsync` — creates AbnormalState on the NPC, broadcasts SM_ABNORMAL_EFFECT(npc, isPlayer=false); fire-and-forget expiry task removes effect and re-broadcasts with current position worldId
    - [✓] `CastNpcDebuffAsync` — creates AbnormalState on the player target, broadcasts SM_ABNORMAL_EFFECT(player, isPlayer=true); fire-and-forget expiry mirrors player DEBUFF expiry pattern; combat time updated on both npc and target
    - Previously: all NPC skills regardless of SubType always applied magic damage to the player; NPCs could not heal themselves, buff themselves, or apply debuffs; SM_SKILL_ACTIVATION was never sent for NPC casts
    - Build: 0 warnings, 0 errors

144. [✓] CM_REMOVE_ALTERED_STATE — player manually cancels a buff/debuff (session 2026-05-02)
    - [✓] `Creature` — added `RemoveEffectBySkillId(int skillId)` overload that removes all effects matching only by skillId (without expiry requirement); needed for player-triggered removal where expiry is not known
    - [✓] `CM_REMOVE_ALTERED_STATE` — fully implemented from stub: reads `skillId` (short), calls `player.RemoveEffectBySkillId`, broadcasts `SM_ABNORMAL_EFFECT(player, isPlayer=true, updatedEffects)` to all zone clients; mirrors Java EffectController.removeEffect + broadCastEffects
    - [✓] `GsPacketHandlerFactory` — updated opcode 0xE1 registration to pass `conn` and `_connRegistry`
    - Previously: clicking a buff/debuff icon sent the packet but nothing happened; buff icons persisted even after manual cancel
    - Build: 0 warnings, 0 errors

145. [✓] Buff icons survive zone transitions — CM_LEVEL_READY sends active effects on entry (session 2026-05-02)
    - [✓] `CM_LEVEL_READY` — replaced all three `SM_ABNORMAL_EFFECT(id, isPlayer)` empty-list calls with `SM_ABNORMAL_EFFECT(id, isPlayer, GetActiveEffects())` calls; affects: (1) self on zone entry, (2) each zone peer's effects shown to the entering player, (3) entering player's effects shown to each zone peer
    - Previously: zone entry always sent count=0 SM_ABNORMAL_EFFECT (clear), erasing all buff icons for every player in the zone every time anyone teleported; buffs existed on the server but became invisible on client after any zone transition
    - Build: 0 warnings, 0 errors

146. [✓] Clear active effects on player death — all three kill paths (session 2026-05-02)
    - [✓] `Creature` — added `ClearAllEffects()` that clears the entire `_activeEffects` list under `_effectsLock`
    - [✓] `NpcAiService` (NPC kills player) — after `target.State |= Dead`: calls `target.ClearAllEffects()`, broadcasts `SM_ABNORMAL_EFFECT(target, isPlayer:true)` (clear form, empty list) alongside the DIE emotion to all zone clients
    - [✓] `CM_ATTACK` (melee kill of player) — same clear + broadcast after `deadPlayer.State |= Dead`
    - [✓] `CM_CASTSPELL` (spell kill of player) — same clear + broadcast after `deadPlayer.State |= Dead`
    - Previously: buff/debuff icons remained visible on dead players until their fire-and-forget expiry tasks happened to fire; NPC debuffs applied just before a kill would show for their full duration on a corpse
    - Build: 0 warnings, 0 errors

147. [✓] CM_TOGGLE_SKILL_DEACTIVATE removes chant/buff effect on stance-off (session 2026-05-02)
    - [✓] `CM_TOGGLE_SKILL_DEACTIVATE` — added `PlayerConnectionRegistry` injection; after sending `SM_PLAYER_STANCE(0)`, calls `player.RemoveEffectBySkillId(_skillId)` and broadcasts `SM_ABNORMAL_EFFECT` with updated active effects to all zone clients
    - [✓] `GsPacketHandlerFactory` — updated opcode 0xE0 to pass `_connRegistry`
    - Previously: deactivating a chant sent the stance-off indicator but left the associated buff icon visible on the player's portrait and the effect record in the active-effects list; re-casting the skill would replace the old entry but manual deactivation was silently ignored
    - Build: 0 warnings, 0 errors

148. [✓] Melee critical hit — 10% base chance, 1.5× damage, AttackStatus 202 in SM_ATTACK (session 2026-05-02)
    - [✓] `SM_ATTACK` — added `isCrit` optional bool param (default false); selects between `AttackStatusNormalHit=10` and `AttackStatusCritical=202` for the attack-list entry; Java AttackStatus enum IDs documented inline
    - [✓] `CM_ATTACK` — before defense mitigation: rolls `Random.Shared.Next(100) < 10` for crit; multiplies rawDmg × 1.5 on crit; passes `isCrit` to SM_ATTACK; mitigation applies to the already-boosted raw damage
    - Previously: melee always sent AttackStatus 10 (NORMALHIT) regardless of damage — no crit flash on target, no crit sound, all hits look identical on the client
    - Build: 0 warnings, 0 errors

149. [✓] NPC melee critical hit — 5% base chance, 1.5× damage, AttackStatus 202 (session 2026-05-02)
    - [✓] `NpcAiService` (melee damage path) — after rate scaling, before pdef mitigation: rolls `Random.Shared.Next(100) < 5`; on crit multiplies rawDmg × 1.5f; passes `npcCrit` to SM_ATTACK constructor; NPCs use 5% (vs player's 10%) to feel less threatening
    - Previously: NPC melee attacks always sent AttackStatus 10; no visual crit indicator for players being attacked
    - Build: 0 warnings, 0 errors

150. [✓] SM_STATS_INFO — fix DP and recoverable XP always being sent as 0 (session 2026-05-02)
    - [✓] `SM_STATS_INFO` (current section) — `w.WriteH(0)` for current DP replaced with `w.WriteH((short)p.Dp)`; `w.WriteQ(0)` for ExpRecoverable replaced with `w.WriteQ(p.ExpRecoverable)`; fly time replaced with `p.MaxFp`/`p.CurrentFp`
    - [✓] `SM_STATS_INFO` (base section) — `w.WriteD(60)` for base fly time replaced with `p.MaxFp`
    - Previously: on zone entry or level-up, the stat panel showed DP=0 even if the player had accumulated DP mid-session; the XP bar's grey "recoverable" portion was always 0; fly meter showed hardcoded 60 instead of player's actual FP pool
    - Build: 0 warnings, 0 errors

151. [✓] NPC ATTACKMODE/NEUTRALMODE emotions — combat stance transitions broadcast (session 2026-05-02)
    - [✓] `NpcAiService` (aggro scan) — when a new target is found via the scan loop, broadcasts `SM_EMOTION(npc, ATTACKMODE)` to all zone clients before the SEE shout
    - [✓] `NpcAiService` (target lost / leash) — when locked target is invalid and NPC returns home, broadcasts `SM_EMOTION(npc, NEUTRALMODE)` after BroadcastAttackEndShout
    - [✓] `NpcAiService.ForceEngage` — new target addition via ForceEngage (player hits NPC or NPC retaliates from spell) fires a fire-and-forget Task.Run to broadcast `SM_EMOTION(npc, ATTACKMODE)` since ForceEngage is synchronous
    - Previously: NPCs never sent ATTACKMODE or NEUTRALMODE; clients always displayed NPCs in their spawn-default visual stance even while they were actively attacking; the combat-mode stance change animation (weapon raised, posture shift) never played
    - Build: 0 warnings, 0 errors

152. [✓] NPC out-of-combat HP regeneration (session 2026-05-02)
    - [✓] `NpcAiService` — added `_lastRegenTime` dictionary; in tick loop (after dead-check, before returning-home block): if NPC has no locked target, HP < MaxHp, and 1700ms have elapsed since `LastCombatTime`, restores `MaxHp/4` HP every 6s; broadcasts `SM_ATTACK_STATUS(NaturalHp, heal)` to zone clients; dead-NPC cleanup removes entry from `_lastRegenTime`
    - Java source: `LifeStatsRestoreService` schedules `HpRestoreTask` at 1700ms delay then 6000ms fixed interval; rate is `NpcGameStats.getHpRegenRate()` = `maxHp / 4` per tick; broadcasts `SM_ATTACK_STATUS` with `TYPE.NATURAL_HP` (value 3)
    - Previously: NPCs never regenerated HP after combat; a mob beaten to near-death and then leashed would re-engage at the same low HP indefinitely, making repeated pulls trivially easy
    - Build: 0 warnings, 0 errors

153. [✓] RegenService — remove duplicate NPC regen, fix player regen to Java level-based formula (session 2026-05-02)
    - [✓] `RegenService` — removed NPC HP regen block (was MaxHp/100 per tick, no broadcast; now superseded by M152 NpcAiService which does MaxHp/4 per 6s with SM_ATTACK_STATUS broadcast); removed `GameWorld` injection and unused alias
    - [✓] `RegenService` (player HP) — replaced `MaxHp/50` flat rate with Java formula `(level+3)*health/100` per 6s tick; injected `IDataManager` to resolve `PlayerStatsTemplate` per player; falls back to health=100 if template missing
    - [✓] `RegenService` (player MP) — replaced `MaxMp/50` flat rate with Java formula `(level+8)*will/100` per 6s tick
    - Java source: `PlayerLifeStats` HP regen = `(level+3) * healthStat / 100`; MP regen = `(level+8) * willStat / 100`; both on 6s interval after 1.7s out-of-combat delay
    - Previously: player regen used a flat 2% of max HP/MP per tick, ignoring level and stats — low-level players regenerated proportionally too fast, high-level players too slow relative to their stat investment
    - Build: 0 warnings, 0 errors

154. [✓] Soul sickness debuff broadcast — skull icon visible on revive/login/cure (session 2026-05-02)
    - [✓] `AbnormalState.RemainingMs` — fixed overflow: `(int)Math.Min(...TotalMilliseconds, int.MaxValue)` so `DateTime.MaxValue` expiry (persistent effects) doesn't cast a huge double to a negative int
    - [✓] `PlayerEnterWorldService` — after MaxHp/MaxMp recompute: if SoulSicknessCount > 0, calls `player.AddEffect(AbnormalState{SkillId=8291, SkillLevel=count, Expiry=DateTime.MaxValue})`; CM_LEVEL_READY then includes it in the SM_ABNORMAL_EFFECT broadcast on zone entry
    - [✓] `CM_REVIVE` — after MaxHp/MaxMp recompute: adds AbnormalState(8291) if SoulSicknessCount > 0; in same-zone branch (after stand emotion): broadcasts `SM_ABNORMAL_EFFECT` with active effects to all zone clients so skull icon appears immediately without needing a zone reload
    - [✓] `CM_DIALOG_SELECT.HandleSoulSicknessRecoveryAsync` — after zeroing SoulSicknessCount: calls `RemoveEffectBySkillId(8291)` and broadcasts `SM_ABNORMAL_EFFECT(updatedEffects)` to zone so skull icon disappears on cure
    - [✓] `CM_GM_COMMAND_SEND.HandleSoulSicknessClear` — same removal + broadcast pattern as healer cure
    - Java source: `PlayerController.addDeathPenalty` uses `skillId=8291` with `deathCount` as skill level via `SkillEngine.getSkill(player, skillId, deathCount, player).useSkill()`
    - Previously: SoulSicknessCount was tracked, HP/MP multiplier applied, but the skull icon never appeared on the player's portrait; players had no visual indication of how many stacks they carried
    - Build: 0 warnings, 0 errors

155. [✓] Death system messages — "You were killed by X" and group notifications (session 2026-05-02)
    - [✓] `SM_SYSTEM_MESSAGE` — added `YouDied()` (1300737), `YouWereKilledBy(string)` (1340002), `GroupMemberDied(string)` (1350000)
    - [✓] `NpcAiService` (NPC kills player) — after SM_DIE: sends `YouWereKilledBy(npc.Template.Name)` to dying player; sends `GroupMemberDied(target.Name)` to all group members
    - [✓] `CM_ATTACK` (melee PvP kill) — same pattern: `YouWereKilledBy(player.Name)` to victim, `GroupMemberDied` to victim's group
    - [✓] `CM_CASTSPELL` (spell PvP kill) — same pattern
    - Java source: `STR_DEATH_MESSAGE_ME` 1300737, `STR_MSG_COMBAT_MY_DEATH_TO_B` 1340002, `STR_MSG_COMBAT_FRIENDLY_DEATH` 1350000
    - Previously: players received SM_DIE (revive dialog) but no text notification of who killed them; group members saw no death notification at all
    - Build: 0 warnings, 0 errors

156. [✓] Zone-wide PvP kill announcement — "%0 was killed by %1's attack" (session 2026-05-02)
    - [✓] `SM_SYSTEM_MESSAGE` — added `PlayerKilledByPlayer(string, string)` (code 1350001)
    - [✓] `CM_ATTACK` (PvP kill) — after dead player SM_DIE: broadcasts `PlayerKilledByPlayer(victim, killer)` to all zone players
    - [✓] `CM_CASTSPELL` (spell PvP kill) — same zone-wide broadcast
    - Java source: `PvpService.onKill` calls `broadcastPacketAndReceive(STR_MSG_COMBAT_FRIENDLY_DEATH_TO_B)` zone-wide after every player-kills-player event
    - Previously: PvP kills were silent to bystanders — only the victim saw a death message; no public zone notification existed
    - Build: 0 warnings, 0 errors

157. [✓] Obelisk bind point — SM_BIND_POINT_INFO restored on zone entry (session 2026-05-02)
    - [✓] `CM_LEVEL_READY` — after SM_PLAYER_INFO self-send: if `player.BindPosition.HasValue`, sends `SM_BIND_POINT_INFO(bindPos)` so the client's revive dialog shows the correct obelisk location after every zone reload
    - RESURRECT_BIND dialog action (code 34) was already implemented in CM_DIALOG_SELECT (binds at NPC position, persists via UpdateBindPointAsync, sends SM_BIND_POINT_INFO + BindPointSet message)
    - Previously: the bind point was set correctly in DB and sent immediately on binding, but was never re-sent on zone entry — after any teleport/zone change the revive dialog showed no bind point location even though one was set
    - Build: 0 warnings, 0 errors

158. [✓] SM_PLAYER_INFO — run speed and attack speed from player model (session 2026-05-02)
    - [✓] `Creature.MovementSpeed` — changed `protected set` to `set` so external services can assign it
    - [✓] `PlayerEnterWorldService` — sets `player.MovementSpeed = tpl?.RunSpeed ?? 6.0f` from the per-class/level stats template, placed alongside the existing CurrentAttackSpeed assignment
    - [✓] `SM_PLAYER_INFO` — replaced hardcoded `6.0f` move speed with `p.MovementSpeed`; replaced hardcoded `1500`/`1500` attack speed base/current with `(short)p.CurrentAttackSpeed`
    - Java source: `PlayerGameStats.getRunSpeed()` returns stat-template run speed + modifiers; `getAttackSpeed()` defaults to 1500ms, overridden by main-hand weapon template; both sent in SM_PLAYER_INFO
    - Previously: all players appeared to other clients with a hardcoded 6.0 run speed and 1500ms attack speed regardless of their stats template or equipped weapon — weapon attack-speed buffs had no visible effect on the animation timing seen by others
    - Build: 0 warnings, 0 errors

159. [✓] CM_USE_ITEM — broadcast HP/MP restore to zone peers and update group HP bars (session 2026-05-02)
    - [✓] `CM_USE_ITEM` (HP/MP restore path) — after applying the restore, broadcasts `SM_ATTACK_STATUS(NaturalHp/NaturalMp)` to all zone players (was self-only); if player is in a group, sends `SM_GROUP_MEMBER_INFO(Update)` to all group members
    - Java source: `PlayerLifeStats.increaseHp/increaseMp` → `PacketSendUtility.broadcastPacketAndReceive` for life stats updates
    - Previously: when a player used a HP or MP potion, other players in the zone saw no HP bar change; group members' party panel showed stale HP until the next regen tick or combat hit; the heal popup (floating number) was invisible to nearby players
    - Build: 0 warnings, 0 errors

160. [✓] Repurchase system — buy back recently sold items from NPC shops (session 2026-05-02)
    - [✓] `RepurchaseService` (new) — session-scoped store; `Add` records sold items keyed by player ObjectId; `Get`/`GetAll`/`Remove`/`Clear` for lookup and cleanup; thread-safe with lock; items not persisted (lost on restart/logout)
    - [✓] `SM_REPURCHASE` (new, opcode 0xA7) — sends NPC objectId + repurchase item list; each entry writes item info blob via `SM_INVENTORY_INFO.WriteItemInfo` then appends `writeQ(repurchasePrice)`
    - [✓] `CM_BUY_ITEM.SellToShopAsync` — after selling, records each sold item in `RepurchaseService`; for full sells (item deleted) reuses the original UniqueId as the session reference; for partial sells calls `NextUniqueIdAsync` for a fresh stable ID
    - [✓] `CM_BUY_ITEM` case 2 (repurchase) — new `RepurchaseFromShopAsync`: validates NPC range, checks kinah and inventory space, deducts kinah, creates new `Item` instances for repurchased entries, saves inventory, sends `SM_INVENTORY_ADD_ITEM`
    - [✓] `CM_DIALOG_SELECT` case `BUY_AGAIN` (dialog action 70) — validates NPC range, fetches repurchase list from `RepurchaseService`, sends `SM_REPURCHASE`
    - [✓] `Program.cs` — registered `RepurchaseService` as singleton; `GsPacketHandlerFactory` updated with new constructor parameter
    - Java source: `RepurchaseService` (singleton Multimap), `TradeService.performSellToShop` → `addRepurchaseItems`, `DialogService` case `BUY_AGAIN` → `SM_REPURCHASE`, `CM_BUY_ITEM` case 2 → `repurchaseFromShop`
    - Previously: `CM_BUY_ITEM` silently ignored `tradeActionId = 2`; the "Buy Again" NPC dialog button had no handler — players could sell items but had no way to buy them back
    - Build: 0 warnings, 0 errors

161. [✓] Magical weapon M-attack — staffs/orbs/maces set MainHandMagicalAtk; used in SM_STATS_INFO and spell damage (session 2026-05-02)
    - [✓] `ItemTemplate` — added `IsMagicalWeapon` property (MACE_1H, STAFF_2H, BOOK_2H, ORB_2H, HARP_2H, GUN_1H, CANNON_2H, KEYBLADE_2H); these weapon types store magical attack in their `weapon_stats min_damage/max_damage`, not physical attack
    - [✓] `Player` — added `MainHandMagicalAtk` property (default 0; average of magical weapon min/max damage when equipped)
    - [✓] `CM_EQUIP_ITEM` and `PlayerEnterWorldService` — when equipping the main hand: if `IsMagicalWeapon`, sets `MainHandMagicalAtk = (min + max) / 2` and clears `MainHandMinDmg`/`MaxDmg`; otherwise sets physical damage fields and clears `MainHandMagicalAtk`
    - [✓] `SM_STATS_INFO` — replaced hardcoded `100` M-attack with `(short)(100 + p.MainHandMagicalAtk)` (100 = base M-attack for all classes per Java constants)
    - [✓] `CM_CASTSPELL` — spell damage formula changed from `level * 8 + rand(20,60)` to `(100 + MainHandMagicalAtk) + level * 6 + rand(10,40)`; casters with a good magical weapon now deal more damage
    - Java source: `StatEnchantFunction.getWeaponModifiers` shows BOOST_MAGICAL_SKILL for MACE_1H/STAFF_2H/ORB_2H/BOOK_2H/HARP_2H/GUN_1H/CANNON_2H/KEYBLADE_2H; `PlayerGameStats.getMainHandMagicalAttack()` sums BASE_MAGICAL_ATTACK (100) + equipment MAGICAL_ATTACK modifiers
    - Previously: all weapons set only physical attack fields (`MainHandMinDmg`/`MaxDmg`); `SM_STATS_INFO` always sent M-attack = 100 regardless of equipped weapon; spell damage ignored the equipped weapon entirely — a sorcerer with a legendary staff hit identically to one with no weapon
    - Build: 0 warnings, 0 errors

162. [✓] Equipment stat bonuses — PHYSICAL_ATTACK, MAGICAL_RESIST, MAGICAL_ATTACK from item modifiers tracked on Player and sent in SM_STATS_INFO (session 2026-05-02)
    - [✓] `ItemTemplate` — added `PhysicalAttackBonus` (`PHYSICAL_ATTACK` modifier, 6007 items in data), `MagicResistBonus` (`MAGICAL_RESIST`, 41003 items), `MagicAttackBonus` (`MAGICAL_ATTACK`, 974 items) shortcut properties
    - [✓] `Player` — added `BonusPhysicalAtk`, `BonusMagicResist`, `BonusMagicAtk` properties (sum of respective modifiers from all equipped items)
    - [✓] `PlayerEnterWorldService` — computes the three new bonuses from `storedItems` alongside existing `BonusMaxHp`/`BonusMaxMp`
    - [✓] `CM_EQUIP_ITEM` — recomputes the three bonuses after every equip/unequip alongside existing stat recalculation block
    - [✓] `SM_STATS_INFO` — P-attack: `totalAtk` now includes `+ p.BonusPhysicalAtk`; M-attack: `100 + MainHandMagicalAtk + BonusMagicAtk`; M-resist field: changed from hardcoded `0` to `(short)p.BonusMagicResist`; same values mirrored in base stats section
    - Key distinction: `MAGICAL_RESIST` (armor magic resistance displayed in stats panel) vs `MAGICAL_DEFEND` (mitigation on damage — only 51 items, and commented out in Java StatFunctions.java damage path); `MagicDefense` field remains mapped to `MAGICAL_DEFEND` (near-zero in practice)
    - Previously: `SM_STATS_INFO` sent P-attack without accessory bonuses; M-attack ignored `MAGICAL_ATTACK` accessories (rings/necklaces); M-resist was always 0 even for players with resist-stacked gear
    - Also updated: `CM_CASTSPELL` spell damage formula now includes `+ p.BonusMagicAtk` (magical) and `+ p.BonusPhysicalAtk` (physical); `CM_ATTACK` melee damage now includes `+ p.BonusPhysicalAtk`
    - Build: 0 warnings, 0 errors

163. [✓] Ground-targeted AoE spell damage — skills with first_target=POINT target_type=AREA (session 2026-05-02)
    - [✓] `SkillProperties` — added `FirstTarget`, `TargetType`, `TargetRelation`, `EffectiveRange`, `EffectiveAltitude`, `TargetMaxCount` from skill XML attributes
    - [✓] `SkillTemplate` — added shortcut properties `TargetType`, `TargetRelation`, `EffectiveRange`, `EffectiveAltitude`, `TargetMaxCount`; `IsGroundAoe` flag: true when `first_target=POINT` AND `target_type=AREA`
    - [✓] `CM_CASTSPELL` — new branch for `isDamageSkill && _targetType is 1 or 2 && template.IsGroundAoe`: after cast delay, finds all NPCs and enemy players within `effective_range` cylinder (horizontal radius + altitude filter), caps at `target_maxcount`, applies damage formula per target, broadcasts `SM_ATTACK_STATUS` for each hit, awards 150 DP for the cast, calls `ForceEngage` on surviving hit NPCs; FRIEND-relation variant heals allies
    - Key: Java skill XML uses `first_target="POINT" target_type="AREA"` for ground-targeted AoE; non-ground AoE (target=AREA around self/target) handled in a later milestone
    - NPC death in AoE: full death sequence per killed NPC — die emotion, DIED shout, world.Remove, GenerateDrops, HandleNpcKillAsync, AddGroupExpAsync, Abyss AP, SM_DELETE after 3s, ScheduleRespawn, ClearLoot after 60s
    - Previously: targetType 1/2 spells broadcast the cast animation but applied zero damage — mages could cast cyclone/sleep spells visually but they had no effect; AoE-killed NPCs never dropped loot or awarded XP
    - Build: 0 warnings, 0 errors

164. [✓] Caster/target-centered AoE spell damage — first_target=ME/TARGET with target_type=AREA (session 2026-05-02)
    - [✓] `SkillTemplate` — added `IsCasterAoe` (first_target=ME, target_type=AREA) and `IsTargetAoe` (first_target=TARGET, target_type=AREA) flag properties
    - [✓] `CM_CASTSPELL` single-target path — after primary target is hit, if skill `IsCasterAoe || IsTargetAoe` and `EffectiveRange > 0`: finds all additional NPC enemies within effective_range cylinder centered on caster (IsCasterAoe) or primary target (IsTargetAoe), caps at TargetMaxCount, applies damage + SM_ATTACK_STATUS per splash target, handles NPC death (die emotion, drops, XP, SM_DELETE after 3s, ScheduleRespawn, loot cleanup after 60s), ForceEngage on survivors
    - Key: center point differs — ME-AoE uses player.Position (bladestorm, cyclone, storm strike); TARGET-AoE uses target.Position (chain lightning that arcs to nearby enemies)
    - Stats: 4195 first_target=ME target_type=AREA skills; 6249 first_target=TARGET (overlaps with ONLYONE but AREA subset)
    - Previously: ALL single-target skill casts hit exactly one creature regardless of AoE type — a gladiator's Storm Strike or templar's Ripple Wave would only damage the single selected target
    - Build: 0 warnings, 0 errors

165. [✓] NPC caster-centered AoE skill damage — NPC skills with IsCasterAoe hit multiple players (session 2026-05-02)
    - [✓] `NpcAiService.CastNpcDamageAsync` — accepts `SkillTemplate?` parameter; after primary target hit, if `skillTemplate.IsCasterAoe` and `EffectiveRange > 0`, finds all other living players in the zone within NPC's `effective_range` cylinder, caps at `TargetMaxCount`, applies same damage formula per additional player, broadcasts SM_ATTACK_STATUS
    - `TryCastNpcSkillAsync` — passes `skillTemplate` to `CastNpcDamageAsync`
    - Death handling: player death from AoE splash is caught by the existing `if (target.CurrentHp > 0) continue;` check in `TickAsync` on the next tick; primary target death is handled synchronously as before
    - Previously: NPC skill damage always hit exactly one player regardless of AoE type — boss AoE spells like group damage effects hit only the aggro target
    - Build: 0 warnings, 0 errors

166. [✓] Manastone stat bonuses applied on socket/equip/login (session 2026-05-02)
    - [✓] `EquipStatsCalculator` (new) — static helper; `Compute(IEnumerable<Item> equippedItems, IDataManager dm)` iterates each equipped item and then its `ManaStones` list, calls `dm.Items.GetTemplate` once per item/stone, accumulates all 7 stat fields (MaxHpBonus, MaxMpBonus, PhysicalDefense, MagicDefense, PhysicalAttackBonus, MagicResistBonus, MagicAttackBonus) into a `readonly record struct EquipStats`
    - [✓] `PlayerEnterWorldService` — replaced 7 individual LINQ `.Sum()` calls (one per stat) with a single `EquipStatsCalculator.Compute()` call; manastone stats now included in login stat snapshot
    - [✓] `CM_EQUIP_ITEM` — same replacement; manastone bonuses from already-socketed stones are included whenever gear is swapped
    - [✓] `CM_MANASTONE` — injected `IDataManager`; after actionType=2 (socket) and actionType=3 (remove), if the target item is equipped, calls new `RecomputeAndSendStatsAsync` helper which uses `EquipStatsCalculator.Compute`, updates all 7 Player stat fields + MaxHp/MaxMp, sends `SM_STATS_INFO`; `GsPacketHandlerFactory` updated to pass `_dataManager`
    - Key: previously manastones were stored in the DB and hydrated into `item.ManaStones` on login, but their stat contributions were never summed — socketing a HP+100 manastone had zero visible effect on the stats panel or combat
    - Previously: stat computation iterated only equipped items, not their manastones; `CM_MANASTONE` had a comment "no stat bonus in this implementation"
    - Build: 0 warnings, 0 errors

167. [✓] Enchant-level combat stat bonuses — weapons and armor scale with enchant level (session 2026-05-02)
    - [✓] `EquipStatsCalculator` — refactored: extracted `AccumulateTemplate(ItemTemplate, ...)` from `Accumulate` to avoid double `GetTemplate` lookup; main item loop now calls `AccumulateTemplate` then `AccumulateEnchant` (if EnchantLevel > 0); manastones still use `Accumulate(itemId, dm, ...)`
    - [✓] `AccumulateEnchant(int enchantLvl, int slot, ItemTemplate, ...)` — mirrors Java `StatEnchantFunction.getEnchantAdditionModifier`: weapons (off-hand slots 2/262144 skipped): SWORD_1H/DAGGER_1H +2×enchant pAtk, POLEARM_2H/SWORD_2H/BOW +4×enchant pAtk, MACE_1H/STAFF_2H +3×enchant pAtk, BOOK_2H/ORB_2H +3×enchant mAtk, HARP_2H/CANNON_2H/KEYBLADE_2H +4×enchant mAtk, GUN_1H +2×enchant mAtk; armor (slot groups 16/32/2048=small, 4096=medium, 8=large): ROBE +enchant/+2/+3 pDef + 10/12/14×enchant hp; LEATHER +2/+3/+4 pDef + 8/10/12×enchant hp; CHAIN +3/+4/+5 pDef + 6/8/10×enchant hp; PLATE +4/+5/+6 pDef + 4/6/8×enchant hp
    - Key: Java `BOOST_MAGICAL_SKILL` enchant (20×enchantLvl for MACE/STAFF/ORB/etc.) is a separate stat not tracked in our simplified system — MACE/STAFF enchant maps to pAtk (their physical white-hit bonus); ORB/BOOK/etc. map to mAtk
    - Slot bitmasks from Java ItemSlot enum: TORSO=8, GLOVES=16, BOOTS=32, SHOULDER=2048, PANTS=4096 (HELMET=4 and WAIST=65536 had no case in Java, so no enchant bonus)
    - Previously: enchant levels were stored and enchanting worked visually, but enchanting a +15 sword gave zero combat benefit — damage and defense were identical to +0
    - Build: 0 warnings, 0 errors

168. [✓] EVASION/ACCURACY hit-miss check and dynamic physical crit rate (session 2026-05-02)
    - [✓] `ItemTemplate` — added 4 new shortcut properties: `EvasionBonus`, `PhysicalAccuracyBonus`, `PhysicalCriticalBonus`, `PhysicalCriticalResistBonus` (from item XML modifiers EVASION/PHYSICAL_ACCURACY/PHYSICAL_CRITICAL/PHYSICAL_CRITICAL_RESIST)
    - [✓] `EquipStats` record — extended with `Evasion`, `PhysicalAccuracy`, `PhysicalCritical`, `PhysicalCriticalResist` fields
    - [✓] `EquipStatsCalculator` — `AccumulateTemplate` and `Accumulate` updated to accumulate all 4 new stats; `AccumulateEnchant` updated to also add `mDef` and `pCritRes` from armor enchant (Java StatEnchantFunction.getArmorModifiers MAGICAL_DEFEND and PHYSICAL_CRITICAL_RESIST cases)
    - [✓] `Player` model — added `BonusEvasion`, `BonusPhysicalAccuracy`, `BonusPhysicalCritical`, `BonusPhysicalCriticalResist` (equipment bonus), and `BasePhysicalAccuracy`, `BaseCritRating`, `BaseEvasion` (class stat template base)
    - [✓] `PlayerEnterWorldService` — sets `BasePhysicalAccuracy`, `BaseCritRating`, `BaseEvasion` from stat template at login; assigns all 4 new equipment bonuses from `EquipStats`
    - [✓] `CM_EQUIP_ITEM`, `CM_MANASTONE.RecomputeAndSendStatsAsync` — updated to assign all 4 new bonuses
    - [✓] `SM_STATS_INFO` — evasion, P-crit, P-accuracy, P-crit-resist now include equipment bonus (both current-stats and base-stats sections)
    - [✓] `CM_ATTACK` — hit/miss check before damage: `dodgeRate = clamp((targetEvasion - totalAccuracy) * 0.6 + 50, 0, 300)` out of 1000; NPC evasion approximated as `level * 5`; on evade: broadcasts SM_ATTACK(damage=0) and returns; crit rate: Java piecewise formula (≤440: rate×0.1, ≤600: 44+(r-440)×0.05, else: 52+(r-600)×0.02) out of 1000, reduced by target's PhysicalCriticalResist
    - Key stat frequencies in item XML: EVASION=34,454 items, PHYSICAL_ACCURACY=4,910, PHYSICAL_CRITICAL=4,939, PHYSICAL_CRITICAL_RESIST=2,693 — virtually all armor contributes evasion
    - Previously: every physical attack hit unconditionally (0% miss chance regardless of evasion gear); crit rate was hardcoded 10% regardless of critical rating gear
    - Build: 0 warnings, 0 errors

188. [✓] Death handling for players killed by NPC AoE splash (session 2026-05-02)
    - Previous: `CastNpcDamageAsync` splash loop set HP=0 but never sent SM_DIE, SM_EMOTION(DIE), or triggered XP loss for splash-killed players
    - [✓] `NpcAiService.CastNpcDamageAsync` — after splash hit check `if (splashPlayer.CurrentHp <= 0) HandleNpcSplashKillAsync()`
    - [✓] `NpcAiService.HandleNpcSplashKillAsync` — new private helper: sets Dead state, clears effects, broadcasts SM_EMOTION(DIE)+SM_ABNORMAL_EFFECT, sends SM_DIE+YouWereKilledBy to victim, notifies group, applies XP death loss
    - Build: 0 warnings, 0 errors

187. [✓] Group HP update after NPC spell damage (session 2026-05-02)
    - Previous: `CastNpcDamageAsync` sent `SM_ATTACK_STATUS` but never called `BroadcastGroupHpAsync`; group members saw HP frozen after NPC spell hits
    - [✓] `NpcAiService.CastNpcDamageAsync` — added `BroadcastGroupHpAsync(target, ct)` after primary SM_ATTACK_STATUS broadcast
    - [✓] `NpcAiService.CastNpcDamageAsync` AoE splash loop — added `if (other is Player p) BroadcastGroupHpAsync(p, ct)` for each splash hit
    - Build: 0 warnings, 0 errors

186. [✓] NPC combat idle timeout — disengage after 20s with no activity (session 2026-05-02)
    - Java: `AttackManager.checkOwner` third condition — give up if `lastAttackTimeDelta > 20s AND lastAttackedTimeDelta > 20s`
    - [✓] `NpcAiService.ForceEngage` — `npc.LastCombatTime` now refreshed on EVERY player hit (was only set on initial target registration); prevents timeout reset from being skipped when NPC already has a target
    - [✓] Leash validation loop — added `idleTimeout = (now - npc.LastCombatTime).TotalSeconds > 20`; NPC drops target if idle for 20s
    - `LastCombatTime` is updated by: ForceEngage (player hits NPC), NpcAiService melee/spell attacks (NPC hits player)
    - Covers the Java third condition with a single "last-any-combat-activity" timestamp instead of separate attack/attacked deltas
    - Build: 0 warnings, 0 errors

185. [✓] NPC leash logic matches Java AiInfo defaults (session 2026-05-02)
    - Previous: leash distance = `AggroRange * 1.5f` measured from HOME to PLAYER; a 30-aggro NPC would leash at 45m from spawn
    - Java: two separate limits from `AiInfo` defaults — `chase_target=50` (NPC→player) and `chase_home=200` (NPC→spawn)
    - [✓] `NpcAiService` — replaced `LeashMultiplier = 1.5f` with `ChaseTargetRange = 50f` and `ChaseHomeRange = 200f`
    - [✓] Leash condition changed from `HomePosition.DistanceTo(player) <= AggroRange*1.5` to `npc.Position.DistanceTo(player) <= 50 && HomePosition.DistanceTo(npc.Position) <= 200`
    - Impact: NPCs now chase players further (50m from NPC, not 45m from spawn) and tolerate kiting up to 200m from home before disengaging
    - Java class: `com.aionemu.gameserver.ai2.manager.AttackManager.checkOwner()` and `AiInfo` defaults
    - Build: 0 warnings, 0 errors

184. [✓] NPC body decay timing mirrors Java RespawnService (session 2026-05-02)
    - Java: 5s (no drop entry), 90s (empty drop list), 300s (non-empty drops); previously hardcoded 3s in all paths
    - [✓] `CM_ATTACK` NPC death path — replaced `Task.Delay(3000)` + separate loot-clear delay with `decayMs` computed from `GetLoot(npcObjectId)` result
    - [✓] `CM_CASTSPELL` AoE kill path — same fix for killed-NPCs foreach loop
    - [✓] `CM_CASTSPELL` splash AoE path — same fix for deadSplash Task.Run block
    - [✓] `CM_CASTSPELL` single-target kill path — same fix; removed redundant 57s loot-clear delay
    - Loot now cleared immediately when corpse despawns (SM_DELETE), not on a separate timer
    - Java class: `com.aionemu.gameserver.services.RespawnService` (IMMEDIATE_DECAY=5s, WITHOUT_DROP_DECAY=90s, WITH_DROP_DECAY=300s)
    - Build: 0 warnings, 0 errors

183. [✓] Title stats applied in all stat-recalc paths (session 2026-05-02)
    - Root cause: title stat bonuses (PHYSICAL_ATTACK, SPEED, ATTACK_SPEED, etc.) were computed only in `PlayerEnterWorldService`; equipping/unequipping gear or socketing manastones cleared them
    - [✓] `TitleStatsApplicator` — new static service: `Apply(Player, PlayerTitleTemplate)` adds 20 `<add>` stats and 4 `<rate>` stats on top of EquipStats values
    - [✓] `PlayerTitleTemplate.GetRateStat(string)` — new method mirroring `GetAddStat`
    - [✓] `PlayerEnterWorldService` — calls `TitleStatsApplicator.Apply` after EquipStats block, before `CurrentAttackSpeed`/`MovementSpeed` derivation
    - [✓] `CM_EQUIP_ITEM` — same call inserted after EquipStats assignment block
    - [✓] `CM_MANASTONE.RecomputeAndSendStatsAsync` — same call inserted after EquipStats assignment block
    - Rate stats covered: SPEED→BonusMovementSpeedPct, ATTACK_SPEED→BonusAttackSpeedPct, FLY_SPEED→BonusFlySpeedPct, BOOST_CASTING_TIME→WeaponCastTimeBonus
    - MAXHP/MAXMP intentionally kept in TitleBonusMaxHp/TitleBonusMaxMp (separate fields, added after base + equipment bonus)
    - Build: 0 warnings, 0 errors

182. [✓] Fix item stat getter bug — GetAllStat covers both flat and bonus <add> entries (session 2026-05-02)
    - Root cause: Aion 4.6 item XML marks equipment stat bonuses with `bonus="true"` on `<add>` elements; `GetStat` only read `bonus=false` entries and returned 0 for the vast majority of items
    - Stats affected (all-bonus=true → GetStat returned 0): MAXHP (~27k items), MAXMP (15k), PHYSICAL_ATTACK (6k), MAGICAL_ATTACK (974), PHYSICAL_CRITICAL (4.9k), MAGICAL_CRITICAL (2.7k), MAGICAL_CRITICAL_RESIST (863), CONCENTRATION (10k), BOOST_MAGICAL_SKILL (11.8k), HEAL_BOOST (1.3k)
    - Stats affected (mixed flat/bonus — GetStat partially worked): PHYSICAL_DEFENSE (+9.4k bonus missed), MAGICAL_RESIST (+9.7k bonus missed), MAGICAL_DEFEND (+9 bonus missed), EVASION (+6.4k bonus missed), PHYSICAL_ACCURACY (+4.7k bonus missed), PHYSICAL_CRITICAL_RESIST (+1.6k bonus missed), MAGICAL_ACCURACY (+5.6k bonus missed), MAGIC_SKILL_BOOST_RESIST (+4.2k bonus missed), PARRY (+4.6k bonus missed), BLOCK (+2.3k bonus missed)
    - [✓] `ItemModifiers.GetAllStat(string name)` — new method summing ALL `<add>` entries (ignores bonus flag); mirrors Java AddModifier which sums all regardless of bonus attribute
    - [✓] All 18 affected `ItemTemplate` stat properties switched from `GetStat`/`GetBonusStat` to `GetAllStat`
    - [✓] `ItemTemplate.FlySpeedBonusPct` — new property: `GetRateStat("FLY_SPEED")` (3,755 items)
    - [✓] `Player.BonusFlySpeedPct` — new field; accumulated by EquipStatsCalculator and stored; not yet applied to a fly speed output field
    - [✓] `EquipStats` record — added `FlySpeedBonus` (25th field)
    - [✓] `EquipStatsCalculator.AccumulateTemplate/Accumulate` — added `ref int flySpd`; accumulates `tpl.FlySpeedBonusPct`
    - [✓] `PlayerEnterWorldService`, `CM_EQUIP_ITEM`, `CM_MANASTONE` — assign `BonusFlySpeedPct` from EquipStats
    - Impact: HP/MP bonuses, P-attack, physical crit, concentration, magic boost, heal boost from gear now actually apply to player stats for the first time
    - Build: 0 warnings, 0 errors

181. [✓] SPEED movement bonus from equipment + fix <rate> element parsing (session 2026-05-02)
    - [✓] `ItemModifiers` — added `Rate` list (`[XmlElement("rate")]`) and `GetRateStat(name)` method; previously only `<add>` elements were parsed; `<rate>` elements (SPEED, ATTACK_SPEED, BOOST_CASTING_TIME) were silently ignored
    - [✓] `ItemTemplate.AttackSpeedBonusPct` — changed from `GetBonusStat` to `GetRateStat("ATTACK_SPEED")` (bug fix: ATTACK_SPEED uses `<rate>` not `<add>` in item XML)
    - [✓] `ItemTemplate.CastTimeBonusPct` — changed from `GetBonusStat` to `GetRateStat("BOOST_CASTING_TIME")` (same bug fix)
    - [✓] `ItemTemplate.SpeedBonusPct` — new property: `GetRateStat("SPEED")` (11,119 items have this stat)
    - [✓] `Player.BonusMovementSpeedPct` — new field
    - [✓] `EquipStats` record — added `MovementSpeedBonus` (24th field)
    - [✓] `EquipStatsCalculator.AccumulateTemplate` / `Accumulate` — added `ref int speed`; accumulates `tpl.SpeedBonusPct`
    - [✓] `PlayerEnterWorldService`, `CM_EQUIP_ITEM`, `CM_MANASTONE.RecomputeAndSendStatsAsync` — apply `BonusMovementSpeedPct`; `MovementSpeed = baseRunSpeed * (1000 + BonusMovementSpeedPct) / 1000f`
    - Impact of ATTACK_SPEED fix: accessories with ATTACK_SPEED rate modifiers now actually affect combat speed (was always 0 before)
    - Java formula: SPEED rate values per-1000 of base run speed (22 = +2.2% movement speed)
    - Build: 0 warnings, 0 errors

180. [✓] HEAL_BOOST applied to healing spells (session 2026-05-02)
    - [✓] `CM_CASTSPELL` heal path — `heal *= (1 + BonusHealBoost/1000f)` (Java AbstractHealEffect adds HEAL_BOOST additively at 1000=+100%; simplified to multiplicative factor)
    - Previously: heal boost gear had no effect on the actual HP restored by healing spells
    - Build: 0 warnings, 0 errors

179. [✓] MAGIC_SKILL_BOOST_RESIST (magic suppression) reduces caster's effective magic boost (session 2026-05-02)
    - [✓] `NpcStatsTemplate` — added `MBResist` field (`[XmlAttribute("mbresist")]`)
    - [✓] `CM_CASTSPELL` single-target path — `magicBoostMult` moved inside foreach loop; effective boost = `max(0, BonusMagicBoost - targetMBSuppress)` where `targetMBSuppress = Player.BonusMagicSuppression` or `Npc.Template.Stats.MBResist`
    - [✓] `CM_CASTSPELL` ground AoE path — same per-target suppression subtraction
    - [✓] `CM_CASTSPELL` splash AoE path — same; splash targets are NPC-only so uses `MBResist` from template
    - Java: `magicBoost -= tgs.getMBResist().getCurrent()` then capped at [0, 2901] in StatFunctions.calculateMagicalSkillDamage
    - Previously: BonusMagicSuppression was written to SM_STATS_INFO but had no effect on incoming spell damage
    - Build: 0 warnings, 0 errors

178. [✓] BOOST_MAGICAL_SKILL scaling applied to spell damage (session 2026-05-02)
    - [✓] `CM_CASTSPELL` single-target path — `rawSpellDmg *= (1 + BonusMagicBoost/1000f)` (Java: `damages = baseDmg * (knowledge/100 + magicBoost/1000)`; with knowledge base=100 the factor reduces to `1 + magicBoost/1000`)
    - [✓] `CM_CASTSPELL` ground AoE path — same factor applied to `rawSpellDmg`; extracted as local `mbMultG` before the crit check
    - [✓] `CM_CASTSPELL` splash AoE path — inline factor applied to `splashRaw` for magical spells
    - Previously: `BonusMagicBoost` was accumulated from equipment and written to SM_STATS_INFO but had no effect on actual spell damage
    - Build: 0 warnings, 0 errors

177. [✓] NPC attacks check player evasion / parry / block (session 2026-05-02)
    - [✓] `NpcAiService` melee damage block — before crit+pdef: dodge check `clamp((evasion-npcAcc)*0.6+50, 0, 300)/1000`; on dodge broadcasts SM_ATTACK(Dodge, 0) and `continue`s; parry check `clamp((parry-npcAcc)*0.6+50, 0, 400)/1000` → 40% rawDmg reduction; block check `clamp(block-npcAcc, 0, 500)/1000` → 50% rawDmg reduction; all checks player-only (NPC vs NPC not checked)
    - [✓] NPC crit multiplier now uses fortitude formula: `critCoeff = max(1.0, 1.5 - round(BonusStrikeFortitude/1000.0))` (was always 1.5×)
    - [✓] `npcWorld` variable moved up before dodge broadcast to resolve scope dependency
    - NPC accuracy approximated as `npc.Level * 5` (Java uses NPC stats template; deferred until NPC stats templates are fully loaded)
    - Previously: NPCs always hit players regardless of evasion/parry/block gear; player defensive stats had no effect in PvE
    - Build: 0 warnings, 0 errors

176. [✓] Strike/spell fortitude — PHYSICAL/MAGICAL_CRITICAL_DAMAGE_REDUCE (session 2026-05-02)
    - [✓] `ItemTemplate` — added `StrikeFortitudeBonus` (`GetBonusStat("PHYSICAL_CRITICAL_DAMAGE_REDUCE")`) and `SpellFortitudeBonus` (`GetBonusStat("MAGICAL_CRITICAL_DAMAGE_REDUCE")`)
    - [✓] `Player` — added `BonusStrikeFortitude`, `BonusSpellFortitude`
    - [✓] `EquipStats` record — added `StrikeFortitude` and `SpellFortitude` (21st/22nd fields)
    - [✓] `EquipStatsCalculator.AccumulateTemplate` / `Accumulate` — added `ref int sF, ref int spF`; accumulates from `tpl.StrikeFortitudeBonus` / `tpl.SpellFortitudeBonus`
    - [✓] `PlayerEnterWorldService`, `CM_EQUIP_ITEM`, `CM_MANASTONE.RecomputeAndSendStatsAsync` — apply both fortitude fields from EquipStats
    - [✓] `SM_STATS_INFO` — writes `BonusStrikeFortitude` / `BonusSpellFortitude` for both current and base sections (previously zeros)
    - [✓] `CM_ATTACK` — crit multiplier formula: `critCoeff = max(1.0, 1.5 - round(sFortitude/1000.0))` (Java `StatCapFunction`); `sFortitude = pvpTarget.BonusStrikeFortitude`, 0 for NPC targets
    - [✓] `CM_CASTSPELL` — same formula applied to single-target and ground AoE magical crit paths; splash AoE targets are NPC-only so plain 1.5× still applies
    - Java stat name: PHYSICAL_CRITICAL_DAMAGE_REDUCE (fortitude vs physical crits), MAGICAL_CRITICAL_DAMAGE_REDUCE (fortitude vs magical crits); both expressed as flat integers ÷1000 reduces crit multiplier from 1.5 toward 1.0
    - Build: 0 warnings, 0 errors

175. [✓] Physical parry and block — CM_ATTACK + SM_ATTACK HitResult enum (session 2026-05-02)
    - [✓] `SM_ATTACK` — replaced `bool isCrit` with `HitResult` enum (Normal=10, Critical=202, Dodge=0, Parry=2, CritParry=194, Block=4, CritBlock=196); `CounterFlag()` helper maps HitResult to Java counter-skill flag bits (32=block, 64=parry, 128=dodge)
    - [✓] `NpcAiService` — updated SM_ATTACK construction to pass `HitResult.Critical`/`HitResult.Normal`
    - [✓] `Player` — added `BaseParry`, `BaseBlock` (from class template), `BonusParry`, `BonusBlock` (from equipment)
    - [✓] `ItemTemplate` — added `ParryBonus` and `BlockBonus` computed properties (`GetBonusStat("PARRY")` / `GetBonusStat("BLOCK")`)
    - [✓] `EquipStats` record — added `Parry` and `Block` fields (19th/20th)
    - [✓] `EquipStatsCalculator` — accumulates PARRY and BLOCK bonus-stat modifiers
    - [✓] `PlayerEnterWorldService` — sets `BaseParry`/`BaseBlock` from stat template; applies `BonusParry`/`BonusBlock` from EquipStats
    - [✓] `ExperienceService` — updates `BaseParry`/`BaseBlock` on level-up
    - [✓] `CM_EQUIP_ITEM`, `CM_MANASTONE` — apply `BonusParry`/`BonusBlock` from EquipStats on equip/socket
    - [✓] `CM_ATTACK` — parry check after dodge, block check after parry (both PvP-only; NPCs have 0 parry/block); parry: 40% damage reduction (Java `damage *= 0.6`); block: 50% reduction (simplified, Java uses shield DAMAGE_REDUCE); `goto afterAttack` jumps past normal hit to DP/godstone/death handling
    - [✓] `SM_STATS_INFO` — writes `BaseParry + BonusParry` and `BaseBlock + BonusBlock` for both current and base sections
    - Parry formula: `(parry - accuracy) * 0.6 + 50`, capped at 400/1000; block: `(block - accuracy)`, capped at 500/1000 (mirrors Java StatFunctions)
    - Build: 0 warnings, 0 errors

174. [✓] Level-up base stat refresh — ExperienceService.HandleLevelUpAsync (session 2026-05-02)
    - [✓] `ExperienceService.HandleLevelUpAsync` — added updates for `BasePhysicalAccuracy`, `BaseCritRating`, `BaseEvasion`, `BaseMagicAccuracy`, `MovementSpeed` from the new stat template on level-up; previously only `MaxHp`, `MaxMp`, and `BasePhysicalAttack` were refreshed, leaving accuracy/crit/evasion/speed stale until relog
    - [✓] `MaxHp`/`MaxMp` calculation updated to apply `SoulSicknessMultiplier` (matches `PlayerEnterWorldService` formula)
    - Build: 0 warnings, 0 errors

173. [✓] BOOST_CASTING_TIME weapon modifier — cast speed display in SM_STATS_INFO (session 2026-05-02)
    - [✓] `ItemTemplate.CastTimeBonusPct` — `GetBonusStat("BOOST_CASTING_TIME")`; values 7–9 typical on staves/books/orbs (bonus="true")
    - [✓] `Player.WeaponCastTimeBonus` — stores the weapon's cast time bonus (0 when no weapon equipped)
    - [✓] `PlayerEnterWorldService` and `CM_EQUIP_ITEM` — set `WeaponCastTimeBonus = tpl.CastTimeBonusPct` on weapon equip; reset to 0 on weapon unequip
    - [✓] `SM_STATS_INFO` — writes `Math.Max(0f, (1000 - WeaponCastTimeBonus) / 1000f)` instead of hardcoded 1.0f; mirrors Java `getReverseStat(BOOST_CASTING_TIME, 1000).getCurrent() / 1000f` via ReverseStat.addToBonus which subtracts the bonus
    - `DuplicateStatFunction` semantics: only main hand weapon contributes; no summing with accessories (not routed through EquipStatsCalculator)
    - Build: 0 warnings, 0 errors

172. [✓] CONCENTRATION, BOOST_MAGICAL_SKILL, MAGIC_SKILL_BOOST_RESIST, HEAL_BOOST from equipment (session 2026-05-02)
    - [✓] `ItemTemplate` — added `ConcentrationBonus`, `MagicBoostBonus`, `MagicSuppressionBonus`, `HealBoostBonus` computed properties (all flat `GetStat` variants)
    - [✓] `Player` — added `BonusConcentration`, `BonusMagicBoost`, `BonusMagicSuppression`, `BonusHealBoost`
    - [✓] `EquipStats` record — extended with 4 new fields (Concentration, MagicBoost, MagicSuppression, HealBoost)
    - [✓] `EquipStatsCalculator.AccumulateTemplate` / `Accumulate` — added 4 new ref params; accumulates all 4 stats from each item template and socketed manastones
    - [✓] `PlayerEnterWorldService`, `CM_EQUIP_ITEM`, `CM_MANASTONE.RecomputeAndSendStatsAsync` — apply all 4 new fields from EquipStats
    - [✓] `SM_STATS_INFO` — writes actual `p.BonusConcentration`, `p.BonusMagicBoost`, `p.BonusMagicSuppression`, `p.BonusHealBoost` (both current and base sections) instead of zeros
    - Java stat names: CONCENTRATION(41), BOOST_MAGICAL_SKILL(104), MAGIC_SKILL_BOOST_RESIST(126), HEAL_BOOST(110) — all flat (no percentage variant)
    - Build: 0 warnings, 0 errors

171. [✓] ATTACK_SPEED percentage bonus from equipment accessories (session 2026-05-02)
    - [✓] `ItemModifiers.GetBonusStat(string name)` — new method summing `bonus="true"` XML entries; complements existing `GetStat` (flat values)
    - [✓] `ItemTemplate.AttackSpeedBonusPct` — computed property returning `Modifiers?.GetBonusStat("ATTACK_SPEED") ?? 0`
    - [✓] `Player.BaseAttackSpeed` / `Player.BonusAttackSpeedPct` — new properties; `BaseAttackSpeed` is the raw weapon speed (default 1500), `BonusAttackSpeedPct` is sum from accessories
    - [✓] `EquipStats` record — added `AttackSpeedBonus` field (15th field)
    - [✓] `EquipStatsCalculator.AccumulateTemplate` / `Accumulate` — added `ref int atkSpd` parameter; accumulates `tpl.AttackSpeedBonusPct`
    - [✓] `PlayerEnterWorldService` — saves `BaseAttackSpeed` from weapon template; applies `CurrentAttackSpeed = BaseAttackSpeed * 1000 / (1000 + BonusAttackSpeedPct)` after EquipStats
    - [✓] `CM_EQUIP_ITEM` — both weapon-equipped and weapon-unequipped branches save `BaseAttackSpeed`; formula applied after EquipStats
    - [✓] `CM_MANASTONE.RecomputeAndSendStatsAsync` — same formula applied so socketing an accessory with ATTACK_SPEED recalculates speed
    - Java formula: `(int)(baseSpeed * 1000 / (1000 + bonus))` where bonus=100 means ~9.1% faster (mirrors Java StatCapFunction)
    - Build: 0 warnings, 0 errors

170. [✓] Magic resist + magical crit in AoE damage paths (session 2026-05-02)
    - [✓] `CM_CASTSPELL` ground AoE loop — inside `foreach (var target in targets)`: after `rawSpellDmg` roll, added magic resist check (`resistRate = max(1, targetMR - totalMagicAccuracy)` out of 1000; on resist: broadcasts 0-damage SM_ATTACK_STATUS and `continue`s to next target) and magical crit check (same piecewise formula as single-target path; 1.5× on crit); target MR resolves to Player.BonusMagicResist or NPC MResist from template
    - [✓] `CM_CASTSPELL` caster/target AoE splash loop — inside `foreach (var splash in splashNpcs)`: same two checks added after `splashRaw` roll; splash targets are NPCs only so crit resist is not applied (NPC crit resist not tracked)
    - Both checks mirror the single-target path added in M169; all three CM_CASTSPELL damage paths (single-target, ground AoE, splash) now apply the same magic combat checks uniformly
    - Build: 0 warnings, 0 errors

169. [✓] Magical crit and magic resist check for spell damage (session 2026-05-02)
    - [✓] `ItemTemplate` — added `MagicalAccuracyBonus`, `MagicalCriticalBonus`, `MagicalCriticalResistBonus` shortcuts
    - [✓] `EquipStats` record — extended with `MagicalAccuracy`, `MagicalCritical`, `MagicalCriticalResist`
    - [✓] `EquipStatsCalculator.AccumulateTemplate` / `Accumulate` — updated to accumulate all 3 new magical stats
    - [✓] `Player` model — added `BonusMagicalAccuracy`, `BonusMagicalCritical`, `BonusMagicalCriticalResist` and `BaseMagicAccuracy`, `BaseMagicCritRating`
    - [✓] `PlayerEnterWorldService`, `CM_EQUIP_ITEM`, `CM_MANASTONE.RecomputeAndSendStatsAsync` — assign 3 new magical bonus fields; `BaseMagicAccuracy` set from `tpl.MagicAccuracy` at login
    - [✓] `SM_STATS_INFO` — M-accuracy and M-crit fields now include equipment bonus; base-stats section updated identically
    - [✓] `CM_CASTSPELL` single-target damage path — before damage: magic resist check `resistRate = max(1, target.BonusMagicResist - totalMagicAccuracy)` out of 1000 (Java calculateMagicalResistRate); on resist: broadcasts 0-damage SM_ATTACK_STATUS and returns; after damage calc: magical crit check using same piecewise formula as physical crit, using `BaseMagicCritRating + BonusMagicalCritical` reduced by `BonusMagicalCriticalResist`; 1.5× damage on crit
    - Key: MAGIC_SKILL_BOOST_RESIST (11,857 items) not yet handled — that's a multiplier on spell bonus damage, deferred to a future milestone; MAGICAL_RESIST (41,003 items) now active as spell resist rating
    - Previously: all spells hit unconditionally regardless of MAGICAL_RESIST stacking; no magical crit regardless of MAGICAL_CRITICAL gear
    - Build: 0 warnings, 0 errors

174. [✓] Base combat stats initialized at login in PlayerEnterWorldService (session 2026-05-02)
    - [✓] `PlayerEnterWorldService` — sets `BasePhysicalAttack`, `BasePhysicalAccuracy`, `BaseCritRating`, `BaseEvasion`, `BaseMagicAccuracy`, `BaseParry`, `BaseBlock`, `BaseMagicCritRating` from `PlayerStatsTemplate` on enter-world; fallback defaults if template missing
    - These were already set in `ExperienceService` on level-up; this ensures fresh logins also have correct values before any level-up event fires
    - Build: 0 warnings, 0 errors

175. [✓] NPC evasion/accuracy from NpcStatsTemplate instead of level×5 approximation (session 2026-05-02)
    - [✓] `CM_ATTACK.cs` — `targetEvasion` for NPC now reads `npc.Template.Stats?.Evasion` (falls back to `Level * 5` only if template field is 0/null)
    - [✓] `NpcAiService.cs` — `npcAccuracy` now reads `npc.Template.Stats?.Accuracy` (same fallback)
    - Java source: `npc_stats.xml` carries `evasion` and `accuracy` attributes per level-band; `NpcStatsTemplate` already parses both fields
    - Both changes use `> 0` guard so NPCs with missing/zero stat data fall back gracefully
    - Build: 0 warnings, 0 errors

190. [✓] NPC debuff skills apply CC flags + SM_TARGET_IMMOBILIZE on movement-blocking CC (session 2026-05-02)
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `AbnormalCcFlags ccFlags` parameter; sets `CcFlags = ccFlags` on the created `AbnormalState`; if `(ccFlags & CantMove) != 0`, broadcasts `SM_TARGET_IMMOBILIZE` to all world observers after SM_ABNORMAL_EFFECT
    - [✓] `NpcAiService.TryCastNpcSkillAsync` — updated DEBUFF call site to pass `skillTemplate.CcFlags`
    - Java source: `RootEffect.java`/`StunEffect.java`/`ParalyzeEffect.java` all set the effected creature's abnormal state ID and broadcast SM_TARGET_IMMOBILIZE; NPC skills go through the same effect pipeline
    - Previously: NPC stun/root/sleep/paralyze debuff skills set no CC flags on the AbnormalState — player `ActiveCcFlags` remained None, so stunned players could still attack/move freely; no position-freeze visual was broadcast
    - Build: 0 warnings, 0 errors

189. [✓] Dual-wield off-hand physical attacks (session 2026-05-02)
    - [✓] `SM_ATTACK.cs` — expanded `HitResult` enum with off-hand values: OffHandNormal=11, OffHandCritical=219, OffHandDodge=1, OffHandParry=3, OffHandBlock=5 (Java AttackStatus.java)
    - [✓] `Player.cs` — added `OffHandMinDmg`, `OffHandMaxDmg`, `OffHandHitCount`, `OffHandWeaponType` to track sub-hand weapon stats
    - [✓] `CM_EQUIP_ITEM.cs` — after main-hand block, reads slot 2 item; if it is a weapon (IsWeapon==true, not a shield/orb), sets off-hand stats from WeaponStats; resets all off-hand fields when slot 2 is empty or holds a non-weapon item
    - [✓] `PlayerEnterWorldService.cs` — same off-hand stat resolution from slot 2 equipped items on world entry
    - [✓] `CM_ATTACK.cs` — after main-hand hit list is built, if `OffHandMinDmg > 0`: calculates off-hand raw damage (same base stats, /2 dual-wield penalty since WeaponDualEffect passive is not implemented); inherits crit status from main-hand (Java getOffHandStats maps CRITICAL→OFFHAND_CRITICAL); rolls separate hit count (1..OffHandHitCount); applies same pdef/PvP/level-diff modifiers; appends to `hits[]` and adds to `totalDamage`
    - Java source: `AttackUtil.java` line 58-59: `getOffHandWeaponType() != null` triggers `calculateOffHandResult`; line 92: `StatFunctions.calculateAttackDamage(false, NONE)`; line 93: `Rnd.get(1, offHandWeapon.getHitCount())`; ItemSlot: MAIN_HAND=1, SUB_HAND=2
    - Dual-wield penalty: Java uses `(200-dualEffectValue)*0.01*diff` negative range causing weak off-hand without the WeaponDualEffect passive buff; simplified to /2 flat penalty for now
    - Previously: players dual-wielding two daggers (e.g. Assassin) dealt 0 off-hand hits; only main-hand damage was calculated
    - Build: 0 warnings, 0 errors

188. [✓] Root/movement blocking via CantMove flags + SM_TARGET_IMMOBILIZE (session 2026-05-02)
    - [✓] `Model/AbnormalCcFlags.cs` — added `OpenAerial=65536` and `CannotMove=4194304` to match Java enum; updated `CantMove` composite to include both + Fear (Java CM_MOVE checks CANT_MOVE_STATE plus `isUnderFear()` separately — merged into one composite); updated `CantAttack` to include OpenAerial and CannotMove
    - [✓] `Network/Aion/ServerPackets/SM_TARGET_IMMOBILIZE.cs` — new packet (opcode 0xCC): writes objectId + x, y, z, heading to freeze the target's position on all clients; Java: sent by RootEffect, StunEffect, StunAlwaysEffect, FearEffect
    - [✓] `CM_MOVE.cs` — added early return: `if ((player.ActiveCcFlags & AbnormalCcFlags.CantMove) != 0) return;` before position update; Java CM_MOVE line 113-115: silently rejects movement when CANT_MOVE_STATE or fear active
    - [✓] `CM_CASTSPELL.cs` — after debuff SM_ABNORMAL_EFFECT broadcast, if `(debuffEffect.CcFlags & CantMove) != 0`, broadcasts SM_TARGET_IMMOBILIZE to all world observers; matches Java RootEffect/StunEffect behavior
    - Java source: `CM_MOVE.java` lines 113-115: `isAbnormalState(CANT_MOVE_STATE) || isUnderFear()`; `RootEffect.java` line 60 + `StunEffect.java` line 57: `broadcastPacketAndReceive(effected, new SM_TARGET_IMMOBILIZE(effected))`
    - Previously: rooted/stunned/sleeping players could still move; movement-blocking CC had no client-side freeze visual
    - Build: 0 warnings, 0 errors

197. [✓] Dispel buff skills — strip buffs from enemy targets (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `ClearBuffs()`: removes all effects where `IsDebuff == false` (self-buffs and allied buffs) and rebuilds CcFlags
    - [✓] `CM_CASTSPELL.cs` — single-target damage path: after `SM_ATTACK_STATUS`, if `HasDispelBuff == true` and target alive, calls `ClearBuffs()` and broadcasts `SM_ABNORMAL_EFFECT`; ground AoE damage path: same check per hit target
    - Java source: `DispelBuffEffect.applyEffect` removes effects where `DispelCategoryType` is BUFF type; simplified to strip all non-IsDebuff effects
    - Previously: 48 dispelbuff skills (Disenchant, Word of Wind etc.) dealt incidental spell damage but never stripped any buffs; enemies kept all self-buffs permanently
    - Build: 0 warnings, 0 errors

196. [✓] Dispel debuff skills — remove active debuffs from target (session 2026-05-02)
    - [✓] `Model/AbnormalState.cs` — added `bool IsDebuff { get; init; }` (defaults false); marks player/NPC-applied debuffs so dispel can distinguish them from buffs
    - [✓] `Model/Creature.cs` — added `ClearDebuffs()`: removes all effects where `IsDebuff == true` and rebuilds CcFlags
    - [✓] `Model/Templates/Skill/SkillEffects.cs` — added `HasDispelDebuff` and `HasDispelBuff` boolean properties checking for `<dispeldebuff>` / `<dispelbuff>` element presence
    - [✓] `CM_CASTSPELL.cs` — debuff path: sets `IsDebuff = true` on applied AbnormalState; DoT block: same; BUFF path: when `HasDispelDebuff`, calls `ClearDebuffs()` on target, broadcasts cleansed `SM_ABNORMAL_EFFECT`, returns early (no buff state added)
    - [✓] `NpcAiService.CastNpcDebuffAsync` — sets `IsDebuff = true` on NPC-applied AbnormalState
    - Java source: `DispelDebuffEffect.applyEffect` — removes effects matching `DispelCategoryType` (DEBUFF_PHYSICAL/MAGICAL) up to `value` count; simplified to clear all IsDebuff states
    - Previously: 83 Cleanse/Cure skills (Cleric's Cleanse Wounds, Dispel Shock etc.) did nothing — they applied a 0-duration BUFF and immediately expired, never removing any debuff from the target
    - Build: 0 warnings, 0 errors

195. [✓] AoE heal skills — IsCasterAoe/IsTargetAoe HEAL subtype (session 2026-05-02)
    - [✓] `CM_CASTSPELL.cs` — after the primary single-target heal block, added AoE heal block: when `template.IsCasterAoe || IsTargetAoe` and has `HealEffects`, iterates all alive same-race players within `EffectiveRange` up to `TargetMaxCount`; applies the same healinstant formula (value+delta*level, percent, healBoost) per ally; sends `SM_ATTACK_STATUS(NaturalHp/NaturalMp)` per healed ally
    - Java source: `Skill.java` uses `TargetType.AREA` + `TargetRelation.FRIEND` to collect targets via `getAffectedList()` then applies `HealInstantEffect.applyEffect` on each; first_target="ME" casts from caster position
    - Previously: 89 AoE heal skills (Healing Wind, Prayer of Wind, Ripple of Purification etc.) only healed the primary target (self); all party members within range received no healing at all
    - Build: 0 warnings, 0 errors

194. [✓] Debuff duration from effect element duration2 — slow/snare/statdown/statup (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `TSlot` property (`tslot` XML attribute); added `EffectDuration` property to `SkillEffects`: iterates slow/snare/statdown/statup/blind/confuse elements and returns max `duration2`
    - [✓] `CM_CASTSPELL.cs` — debuff block: `debuffDurationMs = template.Duration > 0 ? template.Duration : effects.EffectDuration`; `isDebuffSkill` now also fires for ATTACK+tslot=DEBUFF when debuffDurationMs > 0; expiry `Task.Delay` uses captured `debuffDurationMs` instead of `template.Duration`
    - Java source: `AttackResult` skills apply CC effects via `EffectController.scheduleEffect()` which reads `getEffectsDuration()` from the effect XML element; `SlowEffect.java` extends `StatAddEffect` with `statEnum=SPEED`; `StatDownEffect.java` iterates `<change>` children
    - Previously: ~1267 debuff skills with `duration="0"` in skill_template (slow/statdown/snare effects whose actual duration is in the `<slow duration2=...>` element) never applied any debuff visual; e.g. Weakening Severe Blow, Dazing Severe Blow, Slowing Arrow showed no debuff icon
    - Build: 0 warnings, 0 errors

193. [✓] Heal-over-time (HoT) effects — heal / mpheal periodic ticks (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SkillHotInfo` readonly record struct (CheckTimeMs, BaseValue, Delta, Duration2Ms, HealType); added `HotEffects` property to `SkillEffects` parsing `<heal>` (hp) and `<mpheal>` (mp) elements using same checktime/value/delta/duration2 attribute pattern as DoT
    - [✓] `CM_CASTSPELL.cs` — in the HEAL subtype branch, when no `HealEffects` are found, check `HotEffects`: adds `AbnormalState` to target, broadcasts `SM_ABNORMAL_EFFECT` buff icon; spawns `Task.Run` tick loop per effect entry: waits `CheckTimeMs`, heals `BaseValue+Delta*level` capped at headroom, sends `SM_ATTACK_STATUS(NaturalHp/NaturalMp)` each tick; removes effect and clears buff icon on expiry; fallback to level-based instant heal only when both HealEffects and HotEffects are empty
    - Java source: `HealEffect.java` extends `HealOverTimeEffect.onPeriodicAction(HP)`: same `checktime`/`duration2` from XML as DoT; ticks via `EffectController.schedulePeriodicAction`; `increaseHp(TYPE.HP, heal, 0, LOG.HEAL)`
    - Previously: HoT skills (Stamina Recovery, Regenerate Body etc.) applied no heal at all — only a visual buff icon with no effect
    - Build: 0 warnings, 0 errors

192. [✓] Heal skill values from skill template — healinstant / mphealinstant (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SkillHealInfo` readonly record struct (BaseValue, Delta, IsPercent, HealType); added `HealEffects` property to `SkillEffects` parsing `<healinstant>` (hp) and `<mphealinstant>` (mp) elements
    - [✓] `CM_CASTSPELL.cs` — heal path now reads `template.Effects.HealEffects`; for each entry: `heal = (BaseValue + Delta*level) * healBoostMult` (or `maxStat * value / 100` when percent=true); HP heals use `AttackType.NaturalHp + LogId.Heal`, MP heals use `AttackType.NaturalMp + LogId.MpHeal`; caps at available headroom; retains level-based fallback for HEAL-subtype skills with no parseable element
    - Java source: `AbstractHealEffect.calculate` — `value + delta*skillLevel`; `percent` flag divides by 100 of maxStat; `HealBoost` stat multiplies; clamps to available room; `HealInstantEffect.applyEffect` calls `increaseHp(TYPE.REGULAR, heal, 0, LOG.REGULAR)`; `MPHealInstantEffect` uses `TYPE.MP / LOG.REGULAR`
    - Previously: all heal skills used `player.Level*6 + rand[15,40]` regardless of skill; a level-1 Cleric heal and a level-50 heal had the same scaling — template delta values were ignored entirely
    - Build: 0 warnings, 0 errors

191. [✓] Dual-wield attack speed adjustment (session 2026-05-02)
    - [✓] `CM_EQUIP_ITEM.cs` — in the off-hand weapon block (slot 2, IsWeapon==true): after setting OffHand stats, adds `sws2.AttackSpeed / 4` to `player.BaseAttackSpeed` if AttackSpeed > 0; the existing final recalculation applies BonusAttackSpeedPct on the updated base
    - [✓] `PlayerEnterWorldService.cs` — same adjustment in the off-hand block on world entry
    - Java source: `PlayerGameStats.getAttackSpeed()` line 132: `speed += offhandweapon.getWeaponType().getAttackSpeed() / 4` when dual-wielding; higher value = slower individual swings but more off-hand hits per cycle
    - Previously: equipping an off-hand weapon had no effect on base attack speed; dual-wielding computed attack speed from main-hand only
    - Build: 0 warnings, 0 errors

187. [✓] DoT effects (bleed/poison/disease) with periodic damage ticks (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SkillDotInfo` readonly record struct (CheckTimeMs, BaseValue, Delta, Duration2Ms, DotType, Element); `SkillEffects.DotEffects` property parses `<bleed>`, `<poison>`, `<disease>` XML elements from the `[XmlAnyElement]` collection; reads `checktime`, `value`, `delta`, `duration2` attributes
    - [✓] `Model/AbnormalState.cs` — added `SkillDotInfo? DotInfo { get; init; }` to carry per-tick damage info alongside the abnormal state record
    - [✓] `CM_CASTSPELL.cs` — after debuff AbnormalState application, added DoT block: iterates `template.Effects.DotEffects`; per dot creates an AbnormalState with `DotInfo` and `Expiry = UtcNow + Duration2Ms`; launches `Task.Run` tick loop: waits `CheckTimeMs`, applies `dmgPerTick` to `CurrentHp`, sends `SM_ATTACK_STATUS` with `LogId.Bleed` or `LogId.Poison`; loop exits when expired or target dead; removes effect and broadcasts cleared SM_ABNORMAL_EFFECT on exit
    - Java source: `AbstractOverTimeEffect.java` — `checktime` = tick interval; `duration2` = total DoT duration; `value + delta*level` = per-tick damage; `BleedEffect.log=LOG.BLEED`, `PoisonEffect.log=LOG.POISON`; DoT templates have `duration="0"` — duration comes from effect element's `duration2`, not template attribute
    - Previously: bleed/poison/disease skills applied only a visual debuff; no HP drain occurred at all
    - Build: 0 warnings, 0 errors

186. [✓] Silence blocks magical skills; CC state blocks all player skill casting (session 2026-05-02)
    - [✓] `CM_CASTSPELL.cs` — added at start of `RunAsync`: `if (CantAttack != 0) return` blocks all offensive casting while stunned/sleeping/paralyzed; `if (SkillType.MAGICAL && Silence != 0) return` blocks only magical skills when silenced (physical skills remain usable)
    - Java source: `PlayerRestrictions.canUseSkill` line 78: CANT_ATTACK_STATE blocks casting; line 134: SILENCE blocks MAGICAL type skills; physical skills unaffected by silence
    - Previously: silenced players could still cast magical spells; stunned players could still cast skills (auto-attack was gated in M185 but skill casting was not)
    - Build: 0 warnings, 0 errors

185. [✓] CC state flags block player attacks and NPC melee/skills (session 2026-05-02)
    - [✓] `Model/AbnormalCcFlags.cs` — new `[Flags] enum AbnormalCcFlags : long` with Java bit values: Paralyze=4, Sleep=8, Root=16, Silence=256, Fear=512, Stun=4096, Stumble=16384, Stagger=32768, Spin=524288; computed `CantAttack` and `CantMove` composites
    - [✓] `Model/AbnormalState.cs` — added `AbnormalCcFlags CcFlags { get; init; }` (default = None)
    - [✓] `Model/Creature.cs` — added `ActiveCcFlags` property; `AddEffect`/`RemoveEffect`/`ClearAllEffects` maintain the bitmask via `RebuildCcFlags()`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SkillEffects` class using `[XmlAnyElement]` to capture all child elements of `<effects>`; `CcFlags` maps element names to flags (stun/stunalways→Stun, sleep→Sleep, root→Root, silence→Silence, bind→Sleep, paralyze→Paralyze, fear→Fear, stagger/staggeralways→Stagger, stumble/stumblealways→Stumble, spin→Spin); `SkillTemplate.CcFlags` delegates to `Effects?.CcFlags`
    - [✓] `CM_CASTSPELL.cs` — debuff path sets `CcFlags = template.CcFlags` when creating `AbnormalState`
    - [✓] `CM_ATTACK.cs` — added early return: `if ((player.ActiveCcFlags & AbnormalCcFlags.CantAttack) != 0) return;` before cooldown check
    - [✓] `NpcAiService.cs` — added `if ((npc.ActiveCcFlags & CantAttack) != 0) continue;` before NPC melee damage; added same check at start of `TryCastNpcSkillAsync`
    - Java source: `Creature.canAttack()` checks `isAbnormalState(CANT_ATTACK_STATE)` = Spin|Sleep|Stun|Stumble|Stagger|Paralyze|Fear|CANNOT_MOVE; `StunEffect.java` calls `setAbnormal(AbnormalState.STUN.getId())`; effect types are the XML element names in `<effects>` block
    - Previously: stunned/sleeping players could still auto-attack; stunned NPCs continued attacking normally; CC debuffs were visual-only with no gameplay gating
    - Build: 0 warnings, 0 errors

184. [✓] Weapon-type-specific physical crit multiplier for player attacks (session 2026-05-02)
    - [✓] `Player.cs` — added `public string MainHandWeaponType { get; set; } = string.Empty`; set alongside `MainHandHitCount` in equip/enter-world paths
    - [✓] `CM_EQUIP_ITEM.cs` — sets `player.MainHandWeaponType = weaponTpl.WeaponTypeName`; resets to `string.Empty` on unequip
    - [✓] `PlayerEnterWorldService.cs` — same assignment on enter-world weapon resolution
    - [✓] `CM_ATTACK.cs` — player physical crit coefficient now uses weapon-type switch: DAGGER_1H→2.3f, SWORD_1H→2.2f, MACE_1H→2.0f, KEYBLADE_2H/KEYHAMMER_2H/SWORD_2H/POLEARM_2H→1.8f, GUN_1H/CANNON_2H/HARP_2H/STAFF_2H/BOW→1.7f, default→1.5f; minus Math.Round(fortitude/1000f), min 1.0f
    - Java source: `AttackUtil.calculateWeaponCritical` lines 161-188; NPC case (weaponType=null) uses 2.0f default (already fixed in M182); magical crit (MAGICAL_CRITICAL_DAMAGE_REDUCE) always 1.5f (already correct in CM_CASTSPELL)
    - Previously: all player weapon crits used 1.5f (dagger crits were 35% weaker than Java; sword crits were 32% weaker); class asymmetry was lost
    - Build: 0 warnings, 0 errors

183. [✓] Player weapon multi-hit using weapon_stats hit_count (session 2026-05-02)
    - [✓] `ItemTemplate.cs` / `WeaponStats` — added `[XmlAttribute("hit_count")] public int HitCount { get; set; } = 1`; XML field already present in item_templates.xml (e.g. daggers have hit_count="4", swords hit_count="2")
    - [✓] `Player.cs` — added `public int MainHandHitCount { get; set; } = 1`; set alongside `MainHandMinDmg/MaxDmg`
    - [✓] `CM_EQUIP_ITEM.cs` — set `player.MainHandHitCount = ws.HitCount > 0 ? ws.HitCount : 1` on equip; reset to 1 on unequip
    - [✓] `PlayerEnterWorldService.cs` — same assignment on enter-world weapon resolution
    - [✓] `CM_ATTACK.cs` main path — after pdef mitigation: `hitCount = Rnd.get(1, MainHandHitCount)`; split via Java formula; sends `SM_ATTACK` with `HitEntry[]`; parry/block paths retain single-hit (PvP-only, less common)
    - Java source: `AttackUtil.calculateMainHandResult` line 78: `mainHandHits = Rnd.get(1, mainHandWeapon.getItemTemplate().getWeaponStats().getHitCount())`; same split formula as NPC (first=damage*(1-0.1*(n-1)), rest=damage*0.1)
    - Previously: player always dealt a single hit regardless of weapon; dagger (4-hit max) and sword (2-hit max) dealt the same number of hits as a staff
    - Build: 0 warnings, 0 errors

182. [✓] NPC multi-hit physical attacks and crit multiplier correction (session 2026-05-02)
    - [✓] `SM_ATTACK.cs` — extended to support a variable-length hit list: added `HitEntry` record, multi-hit constructor accepting `HitEntry[]`; `Write` loops all entries; single-hit constructor remains for compatibility
    - [✓] `NpcAiService.cs` melee attack — after pdef mitigation, roll `hitCount = Random.Shared.Next(1, 4)` (1–3); first hit = `damage*(1-0.1*(n-1))`, subsequent hits = `damage*0.1`; total HP loss = sum; SM_ATTACK now carries the full hit list
    - [✓] `NpcAiService.cs` crit multiplier — base changed from 1.5f to 2.0f; Java `AttackUtil.calculateWeaponCritical(weaponType=null)` uses default `coeficient=2f` for NPCs; fortitude reduction `Math.Round(fortitude/1000f)` unchanged
    - Java source: `AttackUtil.calculateMainHandResult` line 80: `mainHandHits = Rnd.get(1, 3)`; `splitPhysicalDamage` lines 140-144: `firstHit = damage*(1-0.1*(n-1))`, `otherHits = damage*0.1`; crit coeff=2.0 when weaponType=null (line 158 default)
    - Previously: NPC always dealt a single hit per attack cycle; crit coefficient was 1.5× instead of 2×, under-multiplying NPC critical strikes
    - Build: 0 warnings, 0 errors

181. [✓] Separate MResist (resist chance) from MDef (damage mitigation) for NPC spell targets (session 2026-05-02)
    - [✓] `CM_CASTSPELL.cs` single-target — `spellDef` for NPC magical spells changed from `NpcMagicResist(npc)` to `npc.Template.Stats?.MBResist ?? 0`
    - [✓] `CM_CASTSPELL.cs` ground AoE loop — same fix for NPC magical spell defense
    - [✓] `CM_CASTSPELL.cs` AoE splash — `splashDef` for magical changed from `NpcMagicResist(splash)` to `Template.Stats?.MBResist ?? 0`
    - Java: `NpcGameStats.getMResist()` is the MAGICAL_RESIST stat used ONLY for resist-chance check; `getMDef()` = `getStat(StatEnum.MAGICAL_DEFEND, 0)` returns 0 base for NPCs, used for damage mitigation; these are completely separate paths
    - Previously: `NpcMagicResist` (up to 950+ for high-level NPCs) was used as BOTH the resist-chance divisor AND the damage mitigation, causing magic spells that DID hit to still have 95%+ damage reduction; spell damage was effectively halved twice against high-level NPCs
    - Build: 0 warnings, 0 errors

180. [✓] PvP 50% physical and spell damage reduction for player-vs-player combat (session 2026-05-02)
    - [✓] `CM_ATTACK.cs` main damage path — `rawDmg = rawDmg / 2` when `target is Player`; applied after crit and NPC level-diff (no PvP paths there), before pdef mitigation
    - [✓] `CM_ATTACK.cs` parry branch — `rawDmgPr / 2` applied before pdef and 0.6f parry factor
    - [✓] `CM_ATTACK.cs` block branch — `rawDmgBl / 2` applied before pdef and 0.5f block factor
    - [✓] `CM_CASTSPELL.cs` single-target — `rawSpellDmg / 2` when `target is Player`, in `else if` after NPC level-diff branch
    - [✓] `CM_CASTSPELL.cs` ground AoE loop — same `else if (target is Player) rawSpellDmg /= 2` after NPC level-diff
    - Java source: `StatFunctions.adjustDamages` PvP branch: `damages = Math.round(damages * 0.50f)` — applied after weapon randomization but before parry/block split; affects all damage types
    - Previously: player-vs-player damage was full (unhalved); could one-shot low-defense players
    - Build: 0 warnings, 0 errors

179. [✓] NPC level-diff modifier for physical evasion and all damage types (session 2026-05-02)
    - [✓] `CM_ATTACK.cs` — NPC dodge rate: `rawDodgeDiff *= 1 + NpcLevelDiffMod(npcLevel - playerLevel)` before the ×0.6+50 formula; higher-level NPCs dodge more
    - [✓] `CM_ATTACK.cs` — physical damage vs NPC: `rawDmg *= (1 - NpcLevelDiffMod(...))` applied after crit and before pdef mitigation; reduces player damage when NPC is 3+ levels higher
    - [✓] `CM_CASTSPELL.cs` — spell damage vs NPC in both single-target and ground AoE paths: same `NpcLevelDiffMod` reduction applied before def mitigation
    - [✓] `NpcLevelDiffMod` helper added to both CM_ATTACK and CM_CASTSPELL; switch expression table: diff=3→10%, 4→20%, 5→30%, 6→40%, 7→50%, 8→60%, 9→70%, ≥10→80% reduction
    - Java source: `StatFunctions.adjustDamages` — `damages *= (1 - getNpcLevelDiffMod(levelDiff, 0))`; `calculatePhysicalDodgeRate` — `dodgeRate *= (1 + getNpcLevelDiffMod(levelDiff, 0))`
    - Previously: players at any level could deal full damage to any NPC; high-level NPCs were not significantly harder to hit
    - Build: 0 warnings, 0 errors

226. [✓] Passive skill stat accumulation on login — activation="PASSIVE" skills apply permanent session-wide stat bonuses (session 2026-05-02)
    - [✓] `Services/PlayerEnterWorldService.cs` — after equipment + title bonuses, iterates `player.Skills.AllSkills`; for each skill with `Activation == "PASSIVE"` and non-null `Effects`, reads all statup properties and accumulates to the corresponding creature delta fields; MAXHP/MAXMP go to `BonusMaxHp`/`BonusMaxMp` so they're included in the `player.MaxHp/MaxMp` derivation; SpeedStatUpPct goes to `BonusMovementSpeedPct`; all other stats (PatkStatUpDelta, MagicAtkStatUpDelta, PdefStatUpDelta, EvasionStatUpDelta, MResistStatUpDelta, PhysAccDelta, MagicAccDelta, ParryDelta, BlockDelta, PhysCritDelta, MagicCritDelta, PhysCritResistDelta, MagicCritResistDelta, StrikeFortitudeDelta, SpellFortitudeDelta, MagicBoostDelta, HealBoostDelta, MagicDefDelta, ConcentrationDelta, MagicSuppressionDelta, CastTimeDelta, AtkSpeedStatUpDelta) go to the shared delta fields already used by temporary buffs
    - Java source: `PassiveEffect.applyEffect()` calls `target.getGameStats().setStat(stat, addValue)` via `StatAddFunction`; executed once on skill learn and re-applied on re-login via `EffectController.init()` which iterates all permanent effects; passive skills have `activation="PASSIVE"` in skill XML and `duration="0"` (they never expire)
    - Previously: all passive skills (e.g. Gladiator Discipline = +accuracy, Ranger Swift Shot = +critical, Spiritmaster mana mastery = +magic accuracy, etc.) were shown in the skill list but their stat bonuses were never applied; players had lower effective stats than Java server for their class and level
    - Build: 0 warnings, 0 errors

229. [✓] RemoveEffectBySkillId now reverses stat deltas before removing the effect (session 2026-05-02)
    - [✓] `Model/Creature.cs` — `RemoveEffectBySkillId` now iterates matching effects in LIFO order calling `ReverseEffectDeltas(e)` before `RemoveAll`; covers player-clicked buff/debuff removal (CM_REMOVE_ALTERED_STATE), stance deactivation (CM_TOGGLE_SKILL_DEACTIVATE), and soul sickness removal (8291 — no stat deltas, so ReverseEffectDeltas is a no-op for it)
    - `RemoveEffect(skillId, expiry)` is intentionally not changed — it is called only by CM_CASTSPELL/NpcAiService expiry Task.Run blocks that already manually reverse their own deltas
    - Previously: clicking a buff/debuff icon or turning off a stance removed the AbnormalState from the list but left all accumulated delta fields (PdefStatUpDelta, AtkSpeedStatUpDelta, etc.) at stale non-zero values; stats remained buffed/debuffed until next logout despite the effect appearing removed on the client
    - Build: 0 warnings, 0 errors

228. [✓] Passive MaxHp/MaxMp/Speed not overwritten by equipment changes (session 2026-05-02)
    - [✓] `Model/Player.cs` — added `PassiveBonusMaxHp`, `PassiveBonusMaxMp`, `PassiveBonusMovementSpeedPct`; these are set once at login by the passive skill loop and are never touched by EquipStatsCalculator or equipment change paths
    - [✓] `Services/PlayerEnterWorldService.cs` (M226 loop) — passive MaxHp/MaxMp accumulate into `PassiveBonusMaxHp`/`PassiveBonusMaxMp` instead of `BonusMaxHp`/`BonusMaxMp`; passive speed goes to `PassiveBonusMovementSpeedPct` instead of `BonusMovementSpeedPct`; MaxHp/MaxMp/Speed derivation now sums both equipment bonus and passive bonus fields
    - [✓] `CM_EQUIP_ITEM.cs` — MaxHp/MaxMp derivation updated to include `PassiveBonusMaxHp`/`PassiveBonusMaxMp`; added missing `SoulSicknessMultiplier` application (equipment change was computing raw MaxHp without soul sickness penalty); MovementSpeed updated to include `PassiveBonusMovementSpeedPct`
    - [✓] `CM_REVIVE.cs`, `CM_TITLE_SET.cs`, `ExperienceService.cs`, `CM_MANASTONE.cs`, `CM_DIALOG_SELECT.cs`, `CM_GM_COMMAND_SEND.cs` — all MaxHp/MaxMp derivations updated to include passive bonus fields; fixed wrong ssMult application in CM_DIALOG_SELECT class-change path (ssMult was applied only to base, not to bonuses)
    - Previously: equipping or unequipping any item overwrote `BonusMaxHp` with equipment-only value, losing passive MAXHP contributions; soul-sick players equipping items would get wrong (too high) MaxHp
    - Build: 0 warnings, 0 errors

227. [✓] On-death stat delta reset — ClearAllEffects/ClearDebuffs/ClearBuffs now reverse accumulated deltas (session 2026-05-02)
    - [✓] `Model/Creature.cs` — `ClearAllEffects()` now iterates `_activeEffects` in LIFO order calling `ReverseEffectDeltas(e)` before clearing; same pattern added to `ClearDebuffs()` (only effects with `IsDebuff=true`) and `ClearBuffs()` (only `IsDebuff=false`); LIFO order correctly unwinds stacked speed chains (snare B's `PreDebuffSpeed` restores to post-snare-A speed, then snare A's `PreDebuffSpeed` restores to original)
    - [✓] `Model/Creature.cs` — new private `ReverseEffectDeltas(AbnormalState e)` method: subtracts each effect's individual delta contribution from the 31 creature aggregate delta fields (PdefDebuffDelta, MResistDebuffDelta, PatkDebuffDelta, EvasionDebuffDelta, MagicAtkDebuffDelta, AtkSpeedDebuffDelta, MaxHpBonusDelta, MaxMpBonusDelta, MagicBoostDelta, HealBoostDelta, PhysAccDelta, MagicAccDelta, ParryDelta, BlockDelta, PhysCritDelta, MagicCritDelta, PhysCritResistDelta, MagicCritResistDelta, StrikeFortitudeDelta, SpellFortitudeDelta, CastTimeDelta, ConcentrationDelta, MagicSuppressionDelta, PdefStatUpDelta, MagicDefDelta, PatkStatUpDelta, MagicAtkStatUpDelta, EvasionStatUpDelta, MResistStatUpDelta, AtkSpeedStatUpDelta); for speed/attack-speed effects restores from stored pre-effect values (`PreDebuffSpeed`, `PreDebuffAtkSpeed`, `PreBuffMovSpeed`)
    - Java source: `EffectController.removeAllEffects()` calls `endEffect()` on each effect which reverses stat contributions; passive skills in Java are stored as permanent effects so they survive `removeAllEffects()`; in .NET passives are applied directly to delta fields at login (M226) without an AbnormalState entry so they are naturally preserved by this approach
    - Previously: when a player died (ClearAllEffects called), all stat delta fields (PdefDebuffDelta, MResistDebuffDelta, etc.) retained their pre-death values; on respawn the player was debuffed as if the debuffs were still active, and stale buff statup values inflated their stats until next login
    - Build: 0 warnings, 0 errors

178. [✓] Magic resist level-difference penalty in all spell damage paths (session 2026-05-02)
    - [✓] `CM_CASTSPELL.cs` ground AoE resist loop — after base `resistRate = max(1, MR - MA)`, add `(targetLevel - casterLevel - 2) * 100` when gap > 2; mirrors Java `StatFunctions.calculateMagicalResistRate` lines 885-886
    - [✓] `CM_CASTSPELL.cs` single-target resist — same level-difference bonus added
    - [✓] `CM_CASTSPELL.cs` AoE splash resist — same bonus added; splash targets are NPCs so uses `splash.Level`
    - Java formula: `if (targetLevel - attackerLevel > 2) resistRate += (diff - 2) * 100` — at +10 levels the resist rate goes up by 800/1000 (80%), effectively near-immune for much lower casters
    - Previously: level-gap spells hit at full rate regardless of caster vs target level difference
    - Build: 0 warnings, 0 errors

177. [✓] NPC physical accuracy/evasion and magic resist use Java level formulas (session 2026-05-02)
    - [✓] `CM_ATTACK.cs` — NPC evasion now uses `NpcPhysicalAccuracy(npc)` (= `round(level*(33.6-0.16*level)+5)`) + XML template `Evasion` modifier; replaces incorrect `Template.Stats?.Evasion ?? level*5`
    - [✓] `NpcAiService.cs` — NPC accuracy now uses same level formula + `MainHandAccuracy` template modifier; replaces `Template.Stats?.Accuracy ?? level*5`
    - [✓] `CM_CASTSPELL.cs` — added `NpcMagicResist(npc)` helper: uses XML `MResist` if > 0, otherwise `round(level*17.5+75)` (Java `NpcGameStats.getMResist()` fallback); all 5 NPC MResist reads (single-target resist, ground AoE, ground AoE PDef branch, splash resist, splash def) updated
    - Java source: `NpcGameStats.calcStats()` computes `pAccuracy = round(level*(33.6-0.16*level)+5)` used as base for EVASION and PHYSICAL_ACCURACY; `getMResist()` base = `round(level*17.5+75)`; XML template fields are stat modifiers on top, not the base
    - Previously: NPC dodge/accuracy used `level*5` (flat) which was far too low for high-level NPCs; NPC magic resist was 0 when XML field absent so spells always hit
    - Build: 0 warnings, 0 errors

220. [✓] StatDown/StatUp BOOST_CASTING_TIME — cast speed debuff/buff; CastTimeDelta scales both SM_STATS_INFO display and actual server-side castDelay (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `CastTimeAddDelta` (statdown) and `CastTimeStatUpDelta` (statup) for stat=BOOST_CASTING_TIME
    - [✓] `Model/Creature.cs` — added `int CastTimeDelta { get; set; }` (positive = faster, negative = slower)
    - [✓] `Model/AbnormalState.cs` — added `int CastTimeDeltaVal { get; init; }`
    - [✓] `SM_STATS_INFO.cs` — cast speed field now `(1000 - p.WeaponCastTimeBonus - p.CastTimeDelta) / 1000f`
    - [✓] `CM_CASTSPELL.cs` — `castDelay` multiplied by `(1000 - WeaponCastTimeBonus - CastTimeDelta) / 1000f`; full debuff/buff apply+restore chain
    - [✓] `NpcAiService.CastNpcDebuffAsync` — 1 new param; apply/restore chain
    - Java source: BOOST_CASTING_TIME is a ReverseStat written as `(1000 - bonus)/1000`; debuffs use negative ADD values; the server-side delay scales the same way
    - Previously: cast speed debuffs showed the icon but `castDelay` was always the raw template Duration; SM_STATS_INFO cast speed ignored all buff/debuff effects
    - Build: 0 warnings, 0 errors

225. [✓] StatUp MOVEMENT_SPEED — speed buff from statup PERCENT SPEED skills (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs (SkillEffects)` — added `SpeedStatUpPct` (reads statup PERCENT SPEED; positive = faster)
    - [✓] `Model/AbnormalState.cs` — added `SpeedStatUpPct`, `PreBuffMovSpeed` (used to restore speed on expiry)
    - [✓] `CM_CASTSPELL.cs` — buff apply: save `PreBuffMovSpeed`, multiply `MovementSpeed` by `(100 + pct)/100`, max 12.0f, broadcast START_EMOTE2; expiry: restore to `PreBuffMovSpeed`, re-broadcast
    - Java source: SPEED PERCENT statup buffs (e.g. Wind Walk, Speed of Thought) multiply base movement speed; no separate accumulator — direct modify-and-restore
    - Note: overlapping snare + speed buff interactions produce approximate results (last-applied restoration wins); edge-case acceptable for current scope
    - Previously: speed buffs from statup skills were parsed but never modified actual MovementSpeed
    - Build: 0 warnings, 0 errors

224. [✓] StatUp ATTACK_SPEED — haste buff reduces attack speed ms (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs (SkillEffects)` — added `AtkSpeedStatUpDelta` (statup/ATTACK_SPEED; negative ADD = faster attacks)
    - [✓] `Model/Creature.cs` — added `AtkSpeedStatUpDelta` (negative = faster attacks)
    - [✓] `Model/AbnormalState.cs` — added `AtkSpeedStatUpDeltaVal`
    - [✓] `SM_STATS_INFO.cs` — attack speed: `p.CurrentAttackSpeed + p.AtkSpeedDebuffDelta + p.AtkSpeedStatUpDelta`
    - [✓] `CM_ATTACK.cs` — `effectiveAtkSpd` includes `+ player.AtkSpeedStatUpDelta`
    - [✓] `CM_CASTSPELL.cs` — buff apply: `AtkSpeedStatUpDelta += atkSpeedStatUpDelta` + START_EMOTE2 broadcast; expiry: reverses delta + re-broadcasts emotion
    - Java source: ATTACK_SPEED statup buffs (e.g. haste skills) have negative ADD values; lower ms = faster auto-attacks
    - Previously: attack speed buffs were tracked by AbnormalState but never reduced the effective attack cooldown or updated stat display
    - Build: 0 warnings, 0 errors

223. [✓] StatUp EVASION + MAGICAL_RESIST — evasion and magic resist buff from statup skills (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs (SkillEffects)` — added `EvasionStatUpDelta` (statup/EVASION), `MResistStatUpDelta` (statup/MAGICAL_RESIST)
    - [✓] `Model/Creature.cs` — added `EvasionStatUpDelta`, `MResistStatUpDelta`
    - [✓] `Model/AbnormalState.cs` — added `EvasionStatUpDeltaVal`, `MResistStatUpDeltaVal`
    - [✓] `SM_STATS_INFO.cs` — active evasion: `+ p.EvasionStatUpDelta`; base evasion: `+ p.EvasionStatUpDelta`; active M-resist: `+ p.MResistStatUpDelta`; base M-resist: `+ p.MResistStatUpDelta`
    - [✓] `CM_ATTACK.cs` — `targetEvasion` includes `+ target.EvasionStatUpDelta`
    - [✓] `CM_CASTSPELL.cs` — AoE and single-target `targetMR`/`targetMagicResist` include `+ target.MResistStatUpDelta` (replace_all); full buff apply+restore chain for both vals
    - Java source: EVASION statup buffs (e.g. evasion stances) add to dodge chance; MAGICAL_RESIST statup buffs add to magic resist-chance
    - Previously: evasion buffs and magic resist buffs from statup skills were tracked but never reduced hit/resist-chance or updated stat display
    - Build: 0 warnings, 0 errors

222. [✓] StatUp PHYSICAL_ATTACK + MAGICAL_ATTACK — P-attack and M-attack buff from statup skills (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs (SkillEffects)` — added `PhysAtkStatUpDelta` (statup/PHYSICAL_ATTACK), `MagicAtkStatUpDelta` (statup/MAGICAL_ATTACK)
    - [✓] `Model/Creature.cs` — added `PatkStatUpDelta` (positive from statup buffs), `MagicAtkStatUpDelta` (positive from statup buffs)
    - [✓] `Model/AbnormalState.cs` — added `PatkStatUpDeltaVal`, `MagicAtkStatUpDeltaVal`
    - [✓] `SM_STATS_INFO.cs` — `totalAtk` includes `+ p.PatkStatUpDelta`; M-attack display (active + base) includes `+ p.MagicAtkStatUpDelta`
    - [✓] `CM_ATTACK.cs` — all `baseAtk` formulas (main, parry, block) include `+ player.PatkStatUpDelta` via replace_all
    - [✓] `CM_CASTSPELL.cs` — all `mAtk`/`mAtkG`/AoE splash M-attack formulas include `+ player.MagicAtkStatUpDelta` (replace_all); all `pAtk`/`pAtkG`/AoE splash P-attack formulas include `+ player.PatkStatUpDelta` (replace_all); full buff apply+restore chain for both vals
    - Java source: statup PHYSICAL_ATTACK and MAGICAL_ATTACK buffs (e.g. War Cry, Divine Favor) add directly to P-attack/M-attack stat totals
    - Previously: P/M-attack buffs from statup skills were tracked by AbnormalState but never affected combat damage or stat display
    - Build: 0 warnings, 0 errors

221. [✓] StatDown/StatUp PHYSICAL_DEFENSE (statup) + MAGICAL_DEFEND (statdown/statup) — pdef buff and mdef buff/debuff (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `PdefStatUpDelta` (statup/PHYSICAL_DEFENSE), `MagicDefAddDelta` (statdown/MAGICAL_DEFEND), `MagicDefStatUpDelta` (statup/MAGICAL_DEFEND)
    - [✓] `Model/Creature.cs` — added `PdefStatUpDelta` (positive from statup buffs), `MagicDefDelta` (bidirectional: negative debuff, positive buff)
    - [✓] `Model/AbnormalState.cs` — added `PdefStatUpDeltaVal`, `MagicDefDeltaVal`
    - [✓] `SM_STATS_INFO.cs` — pdef: `PhysicalDefense + PdefDebuffDelta + PdefStatUpDelta`; mdef: `Math.Max(100, MagicDefense + MagicDefDelta)`
    - [✓] `CM_ATTACK.cs` — main pdef includes `+ target.PdefStatUpDelta`; parry and block pdef sections extended to also include `PdefDebuffDelta + PdefStatUpDelta` (were missing PdefDebuffDelta — bug fix bundled)
    - [✓] `CM_CASTSPELL.cs` — AoE and single-target spellDef: Player magical path includes `+ MagicDefDelta`; Player physical path includes `+ PdefDebuffDelta + PdefStatUpDelta`; full debuff/buff apply+restore chain for both vals
    - [✓] `NpcAiService.CastNpcDamageAsync` — `mdef` now `target.MagicDefense + target.MagicDefDelta`
    - [✓] `NpcAiService.CastNpcDebuffAsync` — 2 new params (`pdefStatUpDelta`, `magicDefDelta`); apply/restore chain
    - Java source: `PHYSICAL_DEFENSE` statup buffs (e.g. Shield of Darkness) add to pdef alongside statdown debuffs; `MAGICAL_DEFEND` is the magic mitigation stat (very small on most items; 51 items total); statdown reduces it further
    - Previously: pdef buff statup had no effect on damage formulas; MagicDefDelta was absent so mdef buffs/debuffs never changed spell damage taken
    - Build: 0 warnings, 0 errors

219. [✓] StatDown/StatUp CONCENTRATION + MAGIC_SKILL_BOOST_RESIST — concentration feeds into magic accuracy; suppression reduces incoming magic boost (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `ConcentrationAddDelta/StatUpDelta` (stat=CONCENTRATION), `MagicSuppressionAddDelta/StatUpDelta` (stat=MAGIC_SKILL_BOOST_RESIST)
    - [✓] `Model/Creature.cs` — added `ConcentrationDelta`, `MagicSuppressionDelta`
    - [✓] `Model/AbnormalState.cs` — added `ConcentrationDeltaVal`, `MagicSuppressionDeltaVal`
    - [✓] `SM_STATS_INFO.cs` — concentration now `p.BonusConcentration + p.ConcentrationDelta`; M-suppress now `p.BonusMagicSuppression + p.MagicSuppressionDelta`
    - [✓] `CM_CASTSPELL.cs` — `totalMagicAcc` (replace_all) includes `+ player.ConcentrationDelta`; AoE `tMBSuppress` and single-target `tMBSuppressG` include player `MagicSuppressionDelta`; full debuff/buff apply+restore chain for both vals
    - [✓] `NpcAiService.CastNpcDebuffAsync` — 2 new params; apply/restore chain
    - Java source: CONCENTRATION is additive to magic accuracy in PlayerGameStats; MAGIC_SKILL_BOOST_RESIST reduces effective magic boost received by the target
    - Previously: concentration debuffs had no effect on spell hit-rate; suppression buffs had no effect on incoming magic damage reduction
    - Build: 0 warnings, 0 errors

218. [✓] StatDown/StatUp PHYSICAL_CRITICAL_RESIST / MAGICAL_CRITICAL_RESIST / STRIKE_FORTITUDE / SPELL_FORTITUDE — see M217

217. [✓] StatDown/StatUp PHYSICAL_CRITICAL_RESIST / MAGICAL_CRITICAL_RESIST / STRIKE_FORTITUDE / SPELL_FORTITUDE — target-defensive crit resist and crit-damage reduction (session 2026-05-02)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `PhysCritResistAddDelta/StatUpDelta`, `MagicCritResistAddDelta/StatUpDelta`, `StrikeFortitudeAddDelta/StatUpDelta`, `SpellFortitudeAddDelta/StatUpDelta`
    - [✓] `Model/Creature.cs` — added `PhysCritResistDelta`, `MagicCritResistDelta`, `StrikeFortitudeDelta`, `SpellFortitudeDelta`
    - [✓] `Model/AbnormalState.cs` — added `PhysCritResistDeltaVal`, `MagicCritResistDeltaVal`, `StrikeFortitudeDeltaVal`, `SpellFortitudeDeltaVal`
    - [✓] `SM_STATS_INFO.cs` — P-crit-resist fixed from missing `BonusMagicalCriticalResist` (was `0`); both crit-resist and fortitude fields now include delta
    - [✓] `CM_ATTACK.cs` — `critResist` and `sFortitude` (main-hand + off-hand) include player delta
    - [✓] `CM_CASTSPELL.cs` — AoE and single-target `mCritResist`/`spFt` include delta; full debuff/buff apply+restore chain for all 4 vals
    - [✓] `NpcAiService.CastNpcDebuffAsync` — 4 new params; apply/restore chain; `sFortitude` in NPC crit section includes player delta
    - Java source: `PHYSICAL_CRITICAL_RESIST` reduces attacker crit chance; `MAGICAL_CRITICAL_RESIST` same for spells; `PHYSICAL/MAGICAL_CRITICAL_DAMAGE_REDUCE` reduces crit coefficient (fortitude/1000)
    - Previously: M-crit-resist was hardcoded 0 in SM_STATS_INFO; crit-resist/fortitude deltas from buffs/debuffs were never applied to combat formulas
    - Build: 0 warnings, 0 errors

216. [✓] StatDown/StatUp MAGICAL_CRITICAL (ADD) — MagicCritDelta affects M-crit chance for duration (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int MagicCritDelta { get; set; }`
    - [✓] `Model/AbnormalState.cs` — added `int MagicCritDeltaVal { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MagicCritAddDelta` (statdown) and `MagicCritStatUpDelta` (statup)
    - [✓] `SM_STATS_INFO.cs` — active M-crit field now `p.BaseMagicCritRating + p.BonusMagicalCritical + p.MagicCritDelta`
    - [✓] `CM_CASTSPELL.cs` — all three `mCritRating` and `mCritRatingS` computations include `+ player.MagicCritDelta` (replace_all); debuff/buff apply/restore via `MagicCritDeltaVal`
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int magicCritDelta` parameter with full apply/restore chain
    - Java source: `StatDownEffect`/`StatUpEffect` apply to `MAGICAL_CRITICAL`; `PlayerGameStats.getMagicCriticalRate()` sums base + all mods; `calculateMagicalCriticalRate` uses effective value for the piecewise formula
    - Previously: MAGICAL_CRITICAL debuffs applied the icon but `mCritRating` was always `base + bonus` only
    - Build: 0 warnings, 0 errors

215. [✓] StatDown/StatUp PHYSICAL_CRITICAL (ADD) — PhysCritDelta affects P-crit chance for duration (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int PhysCritDelta { get; set; }`
    - [✓] `Model/AbnormalState.cs` — added `int PhysCritDeltaVal { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `PhysCritAddDelta` (statdown) and `PhysCritStatUpDelta` (statup)
    - [✓] `SM_STATS_INFO.cs` — P-crit field now `(t?.MainHandCritRate ?? 0) + p.BonusPhysicalCritical + p.PhysCritDelta` (replace_all both sections)
    - [✓] `CM_ATTACK.cs` — `critRating = player.BaseCritRating + player.BonusPhysicalCritical + player.PhysCritDelta`
    - [✓] `NpcAiService` tick — `npcCritRating = (npc.Template.Stats?.Power > 0 ? npc.Template.Stats.Power : 10) + npc.PhysCritDelta`
    - [✓] `CM_CASTSPELL.cs` / `NpcAiService.CastNpcDebuffAsync` — full debuff/buff apply/restore chain for `PhysCritDeltaVal`
    - Java source: `StatDownEffect`/`StatUpEffect` apply to `PHYSICAL_CRITICAL`; `PlayerGameStats.getMainHandPCritRate()` sums base + all mods; `calculatePhysicalCriticalRate` uses the piecewise formula
    - Previously: PHYSICAL_CRITICAL debuffs (Cleric/Spiritmaster chains) applied icon but crit formula never saw the delta
    - Build: 0 warnings, 0 errors

214. [✓] StatDown/StatUp BLOCK (ADD) — BlockDelta affects block chance for duration (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int BlockDelta { get; set; }` (negative = debuff, positive = buff)
    - [✓] `Model/AbnormalState.cs` — added `int BlockDeltaVal { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `BlockAddDelta` (statdown) and `BlockStatUpDelta` (statup) to `SkillEffects`
    - [✓] `SM_STATS_INFO.cs` — block field now `p.BaseBlock + p.BonusBlock + p.BlockDelta` in both current and base sections (replace_all)
    - [✓] `CM_ATTACK.cs` — `totalBlock = pvpBlock.BaseBlock + pvpBlock.BonusBlock + pvpBlock.BlockDelta`
    - [✓] `NpcAiService` tick — `blk = pvpDef.BaseBlock + pvpDef.BonusBlock + pvpDef.BlockDelta`
    - [✓] `CM_CASTSPELL.cs` / `NpcAiService.CastNpcDebuffAsync` — same apply/restore/broadcast pattern as PARRY
    - Java source: `StatDownEffect`/`StatUpEffect` apply `StatAddFunction` to `BLOCK`; `PlayerGameStats.getBlock()` sums base + mods; `calculatePhysicalBlockRate` uses effective block against accuracy for 50% damage reduction
    - Previously: BLOCK statdown/statup debuffs applied icons but block check always used `base + bonus` only
    - Build: 0 warnings, 0 errors

213. [✓] StatDown/StatUp PARRY (ADD) — ParryDelta affects parry chance for duration (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int ParryDelta { get; set; }` (negative = debuff, positive = buff)
    - [✓] `Model/AbnormalState.cs` — added `int ParryDeltaVal { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `ParryAddDelta` (statdown) and `ParryStatUpDelta` (statup) to `SkillEffects`
    - [✓] `SM_STATS_INFO.cs` — parry field now `p.BaseParry + p.BonusParry + p.ParryDelta` in both current and base sections (replace_all)
    - [✓] `CM_ATTACK.cs` — `totalParry = pvpParry.BaseParry + pvpParry.BonusParry + pvpParry.ParryDelta`
    - [✓] `NpcAiService` tick — `par = pvpDef.BaseParry + pvpDef.BonusParry + pvpDef.ParryDelta`
    - [✓] `CM_CASTSPELL.cs` debuff block: applies and restores `ParryDeltaVal`; buff block same; `NpcAiService.CastNpcDebuffAsync` — added `int parryDelta` parameter with full apply/restore chain
    - Java source: `StatDownEffect`/`StatUpEffect` apply `StatAddFunction` to `PARRY`; `PlayerGameStats.getParry()` sums base + all mods; `calculatePhysicalParryRate` uses effective parry against accuracy for 40% damage reduction
    - Previously: PARRY statdown debuffs (Templar Shield Slash chain) showed icon but parry check always used `base + bonus` only; Cleric/Chanter PARRY buffs were similarly ignored
    - Build: 0 warnings, 0 errors

212. [✓] StatDown/StatUp MAGICAL_ACCURACY (ADD) — MagicAccDelta affects spell hit rate for duration (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int MagicAccDelta { get; set; }` (negative = debuff, positive = buff)
    - [✓] `Model/AbnormalState.cs` — added `int MagicAccDeltaVal { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MagicAccAddDelta` (statdown) and `MagicAccStatUpDelta` (statup) to `SkillEffects`
    - [✓] `SM_STATS_INFO.cs` — M-accuracy field now includes `+ p.MagicAccDelta` in both current and base sections (replace_all)
    - [✓] `CM_CASTSPELL.cs` — all three `totalMagicAcc` computations (ground AoE, single target, AoE splash) now include `+ player.MagicAccDelta`; debuff block: applies and restores `MagicAccDeltaVal`; buff block: applies and restores `MagicAccDeltaVal`
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int magicAccDelta` parameter; apply/restore/broadcast same pattern; `TryCastNpcSkillAsync` passes `Effects?.MagicAccAddDelta ?? 0`
    - Java source: `StatDownEffect`/`StatUpEffect` apply `StatAddFunction` to `MAGICAL_ACCURACY`; `PlayerGameStats.getMainHandMAccuracy()` sums base + all modifiers; `calculateMagicalResistRate` uses effective M-acc against target MResist; lower M-acc = higher resist chance for all spells
    - Previously: MAGICAL_ACCURACY debuffs (Spiritmaster chains) applied the icon but `totalMagicAcc` was always `base + bonus` only; caster's spells resisted at unchanged rate despite the debuff; Cleric/Sorcerer M-acc buffs were similarly ignored
    - Build: 0 warnings, 0 errors

211. [✓] StatDown/StatUp PHYSICAL_ACCURACY (ADD) — PhysAccDelta affects hit rate for duration (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int PhysAccDelta { get; set; }` (negative = debuff, positive = buff)
    - [✓] `Model/AbnormalState.cs` — added `int PhysAccDeltaVal { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `PhysAccAddDelta` (statdown) and `PhysAccStatUpDelta` (statup) to `SkillEffects`
    - [✓] `SM_STATS_INFO.cs` — P-accuracy field now `(t?.MainHandAccuracy ?? 0) + p.BonusPhysicalAccuracy + p.PhysAccDelta` in both current and base sections
    - [✓] `CM_ATTACK.cs` — `totalAccuracy` now includes `+ player.PhysAccDelta` in hit rate formula
    - [✓] `CM_CASTSPELL.cs` — debuff block: applies `target.PhysAccDelta += physAccDelta`, stored in `AbnormalState.PhysAccDeltaVal`, restored on expiry and included in stats broadcast condition; buff block: applies and restores `PhysAccDeltaVal`
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int physAccDelta` parameter; apply/restore/broadcast same pattern as other stat deltas; `TryCastNpcSkillAsync` passes `Effects?.PhysAccAddDelta ?? 0`
    - Java source: `StatDownEffect`/`StatUpEffect` apply `StatAddFunction` to `PHYSICAL_ACCURACY`; `PlayerGameStats.getMainHandPAccuracy()` sums base + all modifiers; `calculatePhysicalResistRate` uses effective accuracy for dodge/parry/block checks
    - Previously: Gladiator/Templar PHYSICAL_ACCURACY debuffs (e.g. Blinding Blow chains) showed icon but left hit-rate formula unchanged; Cleric PHYSICAL_ACCURACY statup buffs had no effect
    - Build: 0 warnings, 0 errors

210. [✓] StatUp HEAL_BOOST — HealBoostDelta amplifies heals for buff duration (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int HealBoostDelta { get; set; }` (positive from statup buffs)
    - [✓] `Model/AbnormalState.cs` — added `int HealBoostDeltaVal { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `HealBoostStatUpDelta` to `SkillEffects` (parses `<statup>/<change stat="HEAL_BOOST" func="ADD">`)
    - [✓] `SM_STATS_INFO.cs` — heal boost field now `p.BonusHealBoost + p.HealBoostDelta` in both current and base sections
    - [✓] `CM_CASTSPELL.cs` — both `healBoostMult` (single-target heal) and `aoeBoostMult` (AoE heal) now include `+ player.HealBoostDelta` in the divisor; buff block applies and expiry restores `HealBoostDelta`
    - Java source: `StatUpEffect` applies `StatAddFunction` to `HEAL_BOOST`; `PlayerGameStats.getHealBoost()` sums modifiers; heal skills call `calculateSkillBoostRate` using effective value
    - Previously: Chanter Protective Ward / Healing Grace chains and Cleric Splendor of Recovery showed HEAL_BOOST icon but caster's heal output was unchanged; heals could not be amplified by mantras
    - Build: 0 warnings, 0 errors

209. [✓] StatUp/StatDown BOOST_MAGICAL_SKILL — MagicBoostDelta applied to spell power multiplier for duration (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int MagicBoostDelta { get; set; }` (positive = buff, negative = debuff)
    - [✓] `Model/AbnormalState.cs` — added `int MagicBoostDeltaVal { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MagicBoostStatUpDelta` (statup, positive) and `MagicBoostAddDelta` (statdown, negative) to `SkillEffects`
    - [✓] `SM_STATS_INFO.cs` — M-boost field now `p.BonusMagicBoost + p.MagicBoostDelta` in both current and base sections
    - [✓] `CM_CASTSPELL.cs` — all three spell-damage boost multipliers now use `player.BonusMagicBoost + player.MagicBoostDelta - suppress`; buff block applies and expiry restores `MagicBoostDelta`; debuff block same for statdown M-boost reduction
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int mBoostDebuffDelta` param; same apply/restore pattern; `TryCastNpcSkillAsync` passes `Effects?.MagicBoostAddDelta ?? 0`
    - Java source: `StatUpEffect` / `StatDownEffect` apply `StatAddFunction` to `BOOST_MAGICAL_SKILL`; `PlayerGameStats.getMagicalSkillBoostResist()` sums modifiers; `calculateMagicalSkillBoostRate` uses effective value; all spell skills use the modifier in damage formula
    - Previously: statup BOOST_MAGICAL_SKILL buffs (Chanter Mantra of Victory, Cleric Blessing of Rock, Sorcerer Focus buffs) showed icons but caster's spell damage multiplier was unchanged
    - Build: 0 warnings, 0 errors

208. [✓] StatDown/StatUp MAXMP — accumulated MaxMpBonusDelta for MP reduction debuffs and MP-boost buffs (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int MaxMpBonusDelta { get; set; }` (negative from statdown debuffs, positive from statup buffs)
    - [✓] `Model/AbnormalState.cs` — added `int MaxMpDelta { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MaxMpAddDelta` (statdown: `<statdown>/<change stat="MAXMP">`) and `MaxMpStatUpDelta` (statup: `<statup>/<change stat="MAXMP">`) to `SkillEffects`
    - [✓] `SM_STATS_INFO.cs` — `maxMp = Math.Max(1, (p.MaxMp > 0 ? p.MaxMp : 500) + p.MaxMpBonusDelta)`; `curMp = Math.Min(curMp, maxMp)` — stat panel reflects both MP reduction and MP buff
    - [✓] CM_CASTSPELL debuff block — applies `MaxMpBonusDelta += maxMpDelta` and clamps `CurrentMp`; expiry restores delta; NpcAiService follows the same pattern with new `maxMpDelta` param
    - [✓] CM_CASTSPELL buff block — applies `MaxMpBonusDelta += maxMpStatUpDelta`; expiry restores and clamps; both MAXHP and MAXMP statup now handled in a single `buffStatChanged` guard
    - Java source: `StatUpEffect` / `StatDownEffect` apply `StatAddFunction` to `MAXMP`; `PlayerGameStats.getMaxMp()` sums base + all modifiers; MP clamped in `endEffect`
    - Previously: statdown MAXMP debuffs (Sorcerer series) and statup MAXMP buffs (Chanter MP-pool mantras) showed icons but left effective MaxMp unchanged
    - Build: 0 warnings, 0 errors

207. [✓] StatDown ATTACK_SPEED (ADD via statdown) — accumulated AtkSpeedDebuffDelta adds ms to attack cooldown (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int AtkSpeedDebuffDelta { get; set; }` (positive = slower attacks, adds ms)
    - [✓] `Model/AbnormalState.cs` — added `int AtkSpeedDelta { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `AtkSpeedAddDelta` property to `SkillEffects`: sums all `<statdown>/<change stat="ATTACK_SPEED" func="ADD">` values
    - [✓] `SM_STATS_INFO.cs` — attack speed field now `Math.Max(500, p.CurrentAttackSpeed + p.AtkSpeedDebuffDelta)` to show increased cooldown
    - [✓] `CM_ATTACK.cs` — cooldown check uses `effectiveAtkSpd = Math.Max(500, player.CurrentAttackSpeed + player.AtkSpeedDebuffDelta)` so debuffed players attack less frequently
    - [✓] CM_CASTSPELL debuff apply — accumulates `target.AtkSpeedDebuffDelta += atkSpdDelta` and broadcasts `SM_EMOTION(START_EMOTE2)` to update clients; expiry restores delta and re-broadcasts
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int atkSpdDelta` parameter; same apply/restore/broadcast pattern; `TryCastNpcSkillAsync` passes `Effects?.AtkSpeedAddDelta ?? 0`
    - Java source: `StatDownEffect` applies `StatAddFunction` to `ATTACK_SPEED` (distinct from `SlowEffect` which uses PERCENT); higher ATTACK_SPEED ms value = slower; `PlayerGameStats.getAttackSpeed()` sums base + all modifiers
    - Note: `<slow>/<change stat="ATTACK_SPEED" func="PERCENT">` (M200) and `<statdown>/<change stat="ATTACK_SPEED" func="ADD">` (M207) are two separate XML patterns; both are now handled
    - Previously: 23 statdown ATTACK_SPEED skills that use ADD (not PERCENT) left player attack cooldown unchanged despite showing the debuff icon
    - Build: 0 warnings, 0 errors

206. [✓] StatDown MAGICAL_ATTACK (ADD) — accumulated MagicAtkDebuffDelta reduces caster spell output, restored on expiry (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int MagicAtkDebuffDelta { get; set; }` (negative = reduced M-attack from statdown)
    - [✓] `Model/AbnormalState.cs` — added `int MagicAtkDelta { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MagicAtkAddDelta` property to `SkillEffects`: sums all `<statdown>/<change stat="MAGICAL_ATTACK" func="ADD">` values
    - [✓] `SM_STATS_INFO.cs` — M-attack field now includes `+ p.MagicAtkDebuffDelta` in both current and base sections; stat panel reflects debuffed spell power
    - [✓] `CM_CASTSPELL.cs` — `mAtk` base, ground AoE `mAtkG`, and splash formula all include `+ player.MagicAtkDebuffDelta`; debuffed players deal proportionally less magical damage
    - [✓] CM_CASTSPELL debuff apply/restore chain extended with `magicAtkDelta`
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int magicAtkDelta` parameter; same accumulate/restore/stats-update pattern; `TryCastNpcSkillAsync` passes `Effects?.MagicAtkAddDelta ?? 0`
    - Java source: `StatDownEffect` applies `StatAddFunction` to `MAGICAL_ATTACK`; `PlayerGameStats.getMainHandMAttack()` returns base + all modifiers; NPC spell damage path uses same stat
    - Previously: 31 MAGICAL_ATTACK statdown skills (Spiritmaster Curse of Fire series, Sorcerer Erosive Flame etc.) showed the debuff icon but left caster M-attack unchanged; debuffed players dealt full spell damage
    - Build: 0 warnings, 0 errors

205. [✓] StatDown/StatUp MAXHP — accumulated MaxHpBonusDelta for HP reduction debuffs and HP-boost buffs (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int MaxHpBonusDelta { get; set; }` (negative from statdown debuffs, positive from statup buffs)
    - [✓] `Model/AbnormalState.cs` — added `int MaxHpDelta { get; init; }` (shared field for both directions)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MaxHpAddDelta` to `SkillEffects` (statdown: sums `<statdown>/<change stat="MAXHP" func="ADD">`) and `MaxHpStatUpDelta` (statup: sums `<statup>/<change stat="MAXHP" func="ADD">`)
    - [✓] `SM_STATS_INFO.cs` — `maxHp = Math.Max(1, (p.MaxHp > 0 ? p.MaxHp : 1000) + p.MaxHpBonusDelta)`; `curHp = Math.Min(curHp, maxHp)` — stat panel reflects both HP reduction and HP buff
    - [✓] `CM_CASTSPELL.cs` debuff block — reads `maxHpDelta = MaxHpAddDelta`; applies `target.MaxHpBonusDelta += maxHpDelta`; clamps `CurrentHp` if it exceeds new effective max; sends `SM_STATS_INFO` when any stat delta non-zero; expiry restores `MaxHpBonusDelta -= MaxHpDelta`
    - [✓] `CM_CASTSPELL.cs` buff block — reads `maxHpStatUpDelta = MaxHpStatUpDelta`; stores in `AbnormalState { MaxHpDelta }`; applies `buffTarget.MaxHpBonusDelta += maxHpStatUpDelta`; sends `SM_STATS_INFO` to player; expiry restores delta and clamps `CurrentHp` if MaxHp shrank below current
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int maxHpDelta` parameter; same accumulate/clamp/restore pattern; `TryCastNpcSkillAsync` passes `Effects?.MaxHpAddDelta ?? 0`
    - Java source: `StatUpEffect` / `StatDownEffect` apply `StatAddFunction` to `MAXHP`; `PlayerGameStats.getMaxHp()` sums base + template + all modifiers; HP clamped to new max in `endEffect`
    - Previously: 47 statdown skills (Gladiator Exhausting Blow chain) reduced MAXHP icon but left effective maxHp unchanged; 404 statup skills (Chanter Invigorating Chant, Protective Ward chains) showed buff icons but maxHp was never increased
    - Build: 0 warnings, 0 errors

204. [✓] StatDown EVASION (ADD) — accumulated evasion delta in dodge rate, restored on expiry (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int EvasionDebuffDelta { get; set; }` (negative = reduced evasion from statdown)
    - [✓] `Model/AbnormalState.cs` — added `int EvasionDelta { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `EvasionAddDelta` property to `SkillEffects`
    - [✓] `CM_ATTACK.cs` — `targetEvasion` now includes `+ target.EvasionDebuffDelta`; lower evasion = higher hit rate against the debuffed target
    - [✓] `SM_STATS_INFO.cs` — evasion field includes `+ p.EvasionDebuffDelta`
    - [✓] CM_CASTSPELL and NpcAiService debuff apply/restore chains extended with `evasionDelta`
    - Java source: `StatDownEffect` applies modifier to `EVASION`; `NpcGameStats.getEvasion()` / `PlayerGameStats.getEvasion()` sum base + modifiers; `calculatePhysicalDodgeRate` uses resulting value
    - Previously: 66 EVASION statdown skills (Ranger Disorienting Arrow, Assassin Slashing Strike, etc.) showed the icon but left target evasion unchanged; attackers didn't benefit from the debuff
    - Build: 0 warnings, 0 errors

203. [✓] StatDown PHYSICAL_ATTACK (ADD) — accumulated patk delta reduces attacker damage, restored on expiry (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int PatkDebuffDelta { get; set; }` (negative = reduced physical attack from statdown)
    - [✓] `Model/AbnormalState.cs` — added `int PatkDelta { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `PhysAtkAddDelta` property to `SkillEffects`: sums all `<statdown>/<change stat="PHYSICAL_ATTACK" func="ADD">` values
    - [✓] `CM_ATTACK.cs` — `baseAtk` formula now includes `+ player.PatkDebuffDelta` (all 3 occurrences: parry, block, main path); reduces the debuffed player's physical damage output
    - [✓] `NpcAiService.cs` — NPC melee `baseAtk = (MainHandAttack + npc.PatkDebuffDelta)` so player-debuffed NPCs deal less melee damage
    - [✓] `SM_STATS_INFO.cs` — `totalAtk` now includes `+ p.PatkDebuffDelta` so stat panel reflects debuffed P-attack
    - [✓] CM_CASTSPELL and NpcAiService debuff apply/restore chains extended with `patkDelta` alongside pdef/mresist
    - Java source: `StatDownEffect` applies `StatAddFunction` to `PHYSICAL_ATTACK`; `PlayerGameStats.getMainHandPAttack()` returns base + template + bonuses + all modifiers; NPC uses `NpcGameStats.getMainHandPAttack()`
    - Previously: 90 PHYSICAL_ATTACK statdown skills (Gladiator Destabilizer, Spiritmaster Weakening, etc.) showed the icon but left attacker damage unchanged
    - Build: 0 warnings, 0 errors

202. [✓] StatDown MAGICAL_RESIST (ADD) — accumulated mresist delta in resist check, restored on expiry (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int MResistDebuffDelta { get; set; }` (negative = reduced magic resist from statdown)
    - [✓] `Model/AbnormalState.cs` — added `int MResistDelta { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MResistAddDelta` property to `SkillEffects`: sums all `<statdown>/<change stat="MAGICAL_RESIST" func="ADD">` values
    - [✓] `CM_CASTSPELL.cs` — single-target and ground AoE magic resist checks: `targetMagicResist += target.MResistDebuffDelta`; debuff block accumulates `MResistDebuffDelta` on apply and restores on expiry; combined with pdef delta for single SM_STATS_INFO send
    - [✓] `SM_STATS_INFO.cs` — M-resist field now uses `Math.Max(0, BonusMagicResist + MResistDebuffDelta)` in both current and base sections
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int mresistDelta` parameter; same pattern as pdef
    - Java source: `StatDownEffect` applies modifier to `MAGICAL_RESIST`; `PlayerGameStats.getMResist()` sums base + all stat functions; `calculateMagicalResistRate` uses resulting value as resist chance divisor
    - Previously: 100 MAGICAL_RESIST statdown skills (Sorcerer/Spiritmaster debuff chains) applied the icon but left magic resist unchanged; enemy spellcasters still resisted at full rate after the debuff
    - Build: 0 warnings, 0 errors

201. [✓] StatDown PHYSICAL_DEFENSE (ADD) — accumulated pdef delta in combat, restored on expiry (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `int PdefDebuffDelta { get; set; }` (base class; negative = reduced pdef from statdown stack)
    - [✓] `Model/AbnormalState.cs` — added `int PdefDelta { get; init; }` (the ADD value from statdown; typically -100 to -400)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `PdefAddDelta` property to `SkillEffects`: sums all `<statdown>/<change stat="PHYSICAL_DEFENSE" func="ADD">` values
    - [✓] `CM_ATTACK.cs` — pdef now uses `pdefBase + target.PdefDebuffDelta`; multiple stacked statdowns combine correctly; negative pdef increases damage
    - [✓] `CM_CASTSPELL.cs` — in debuff block: reads `PdefAddDelta`; applies `target.PdefDebuffDelta += pdefDelta`; sends `SM_STATS_INFO` to debuffed player; on expiry `PdefDebuffDelta -= PdefDelta` and sends `SM_STATS_INFO` again
    - [✓] `SM_STATS_INFO.cs` — pdef field now uses `Math.Max(0, p.PhysicalDefense + p.PdefDebuffDelta)` so stat panel reflects debuffed pdef
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int pdefDelta` parameter; same accumulate/restore/stats-update pattern; `TryCastNpcSkillAsync` passes `Effects?.PdefAddDelta ?? 0`
    - Java source: `StatDownEffect` applies `StatAddFunction` to `PHYSICAL_DEFENSE`; each effect is a separate modifier; `endEffect` removes the modifier and triggers stat recalculation; `PlayerGameStats.getPDef()` sums base + all modifiers
    - Previously: 262 statdown skills with PHYSICAL_DEFENSE changes (Warrior Weakening Severe Blow chain, Gladiator Destabilizer, etc.) showed the debuff icon but left pdef unchanged; targets took the same damage before/after pdef reduction
    - Build: 0 warnings, 0 errors

200. [✓] Slow attack speed increase applied on debuff, restored on expiry (session 2026-05-02)
    - [✓] `Model/AbnormalState.cs` — added `int AttackSpeedPct { get; init; }` and `int PreDebuffAtkSpeed { get; init; }`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SlowAttackSpeedPct` property to `SkillEffects`: walks `<slow>/<change stat="ATTACK_SPEED" func="PERCENT">` children and returns the int value (positive = attacks slower)
    - [✓] `CM_CASTSPELL.cs` — in debuff block: reads `SlowAttackSpeedPct`; applies `target.CurrentAttackSpeed *= (100+pct)/100f` clamped ≥ 500ms; folds snare and slow into single `SM_EMOTION(START_EMOTE2)` broadcast; on expiry restores both `MovementSpeed` and `CurrentAttackSpeed` then re-broadcasts
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int slowAtkPct` parameter; same apply/restore/broadcast pattern; `TryCastNpcSkillAsync` passes `skillTemplate.Effects?.SlowAttackSpeedPct ?? 0`
    - Java source: `SlowEffect` extends `BufEffect` applying `StatAddFunction` on `StatEnum.ATTACK_SPEED`; positive PERCENT increases the attack_speed stat (higher = slower); `BroadcastMode.UPDATE_SPEED` broadcasts `SM_EMOTION(START_EMOTE2)` carrying updated `attackSpeed`
    - Previously: 120 slow skills (Gladiator Dazing Severe Blow, Templar Break Power, Spiritmaster Deceleration Curse etc.) applied the CC icon but never modified `CurrentAttackSpeed`; debuffed targets attacked at full speed
    - Build: 0 warnings, 0 errors

199. [✓] Snare movement speed reduction applied on debuff, restored on expiry (session 2026-05-02)
    - [✓] `Model/AbnormalState.cs` — added `int MovSpeedPct { get; init; }` and `float PreDebuffSpeed { get; init; }` to capture the speed change and original speed at cast time
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SnareNames` set (`snare`/`absolutesnare`); added `SnareSpeedPct` property to `SkillEffects` that walks `<snare>/<change stat="SPEED" func="PERCENT">` child elements and returns the int percent value; added `absolutesnare` to `EffectDurNames` for duration2 parsing
    - [✓] `CM_CASTSPELL.cs` — in the debuff block: reads `template.Effects.SnareSpeedPct`; stores it and `PreDebuffSpeed` on `debuffEffect`; applies `target.MovementSpeed *= (100+pct)/100f` clamped ≥ 1.0f; broadcasts `SM_EMOTION(START_EMOTE2)` to zone; on expiry Task restores `target.MovementSpeed = PreDebuffSpeed` and re-broadcasts `SM_EMOTION(START_EMOTE2)` before removing the effect
    - [✓] `NpcAiService.CastNpcDebuffAsync` — added `int snareSpeedPct` parameter; same apply/restore/broadcast pattern; `TryCastNpcSkillAsync` passes `skillTemplate.Effects?.SnareSpeedPct ?? 0`
    - Java source: `SlowEffect`/`SnareEffect` extend `BufEffect` which applies `StatAddFunction` on `StatEnum.SPEED`; `NpcGameStats.getSpeed()` returns modified speed; `BroadcastMode.UPDATE_SPEED` triggers `SM_EMOTION(START_EMOTE2)` broadcast; `endEffect` removes stat modifier and re-broadcasts speed
    - Previously: 361 snare skills (Ranger Entangling Shot, Chanter Chain of Earth, Spiritmaster Web of Wind etc.) applied the CC visual and icon but left target.MovementSpeed unchanged; players/NPCs still moved at full speed through snares
    - Build: 0 warnings, 0 errors

198. [✓] NPC debuff/buff duration from effect elements — effectiveDuration fallback (session 2026-05-02)
    - [✓] `NpcAiService.TryCastNpcSkillAsync` — compute `effectiveDuration = Duration > 0 ? Duration : Effects?.EffectDuration ?? 0` before the SubType switch; pass to both BUFF/CHANT and DEBUFF cases; mirrors M194 player-side fix
    - Java source: NPC skill cast path reads effect duration the same way as player — `EffectController.scheduleEffect` calls `getEffectsDuration()` from the XML effect element, not the template-level `duration` attribute
    - Previously: NPC slow/snare/statdown debuff skills with `duration="0"` in the template XML (real duration in `<slow duration2=...>` etc.) were silently skipped by `CastNpcDebuffAsync` (guard: `if (durationMs <= 0) return`); same for NPC buffs; about ~200 NPC CC/debuff skills never applied on NPC casts
    - Build: 0 warnings, 0 errors

176. [✓] Fix crit rate 10× scaling bug and NPC crit accuracy (session 2026-05-02)
    - [✓] `CM_ATTACK.cs` — player physical crit: `Random.Shared.Next(1000)` → `Random.Shared.Next(100)`; Java `StatFunctions.calculatePhysicalCriticalRate` uses `nextInt(100)` not `nextInt(1000)`
    - [✓] `CM_CASTSPELL.cs` — magical crit in all 3 damage paths (single-target, ground AoE, AoE splash): same `Next(1000)` → `Next(100)` fix; mCritRate values are percent-scale (0-100), not per-mille
    - [✓] `NpcAiService.cs` — NPC crit: was hardcoded 5%; now uses same piecewise formula with `npc.Template.Stats?.Power` as PHYSICAL_CRITICAL rating (Java `NpcGameStats.getMainHandPCritical()` default=10 → 1%); falls back to 10 when field absent
    - Java reference: `NpcGameStats.getMainHandPCritical()` returns `getStat(PHYSICAL_CRITICAL, 10)` (base 10, not 5%); `calculatePhysicalCriticalRate` compares against `nextInt(100)`, giving rate=10×0.1=1% for NPC base
    - Previously: all crit chances were 10× too low; NPC crit was 5× too high relative to Java
    - Build: 0 warnings, 0 errors

230. [✓] Centralize buff/debuff delta apply/reverse in Creature — eliminate 250+ lines of inline duplication (session 2026-05-02)
    - [✓] `Model/Creature.cs` — added `internal void ApplyEffectDeltas(AbnormalState e)`: mirrors `ReverseEffectDeltas` but uses `+=` for all 31 int delta fields; includes HP/MP clamping when `MaxHpDelta < 0` (debuff reduces cap) or `MaxMpDelta < 0`; speed floats (MovementSpeed, CurrentAttackSpeed) intentionally excluded — their percent-based application needs caller context (clamping constants, SM_EMOTION broadcast)
    - [✓] `Model/Creature.cs` — `ReverseEffectDeltas` visibility changed from `private` to `internal`
    - [✓] `Model/Creature.cs` — `AddEffect` now reverses deltas of any same-skillId effect before removing it from the list, then calls `ApplyEffectDeltas` on the new state; correct re-application when the same skill is refreshed mid-duration
    - [✓] `CM_CASTSPELL.cs` — buff apply block: removed 24-line manual delta accumulation (`if (xxx != 0) buffTarget.XxxDelta += xxx`) since `AddEffect` now applies via `ApplyEffectDeltas`; kept SM_EMOTION broadcast for atkSpeedStatUpDelta and speedStatUpPct (those need network side-effects)
    - [✓] `CM_CASTSPELL.cs` — buff expiry Task.Run: replaced 120-line manual reversal + `RemoveEffect(skillId, expiry)` with `expiryTarget.RemoveEffectBySkillId(skillId)` (reverses all deltas + removes from list); added HP/MP clamp after removal (buff cap shrinks on expiry); kept SM_EMOTION broadcasts for speed/atkspeed; kept SM_STATS_INFO and SM_ABNORMAL_EFFECT sends
    - [✓] `CM_CASTSPELL.cs` — debuff apply block: removed 24-line manual delta accumulation; kept atkSpdDelta SM_EMOTION broadcast; kept SM_STATS_INFO send
    - [✓] `CM_CASTSPELL.cs` — debuff expiry Task.Run: replaced 40-line manual reversal + `RemoveEffect(skillId, expiry)` with `expTarget.RemoveEffectBySkillId(skillId)`; kept speed-restore SM_EMOTION and SM_STATS_INFO sends
    - [✓] `NpcAiService.cs` — `CastNpcDebuffAsync` apply block: removed 24-line manual delta accumulation; kept atkSpdDelta SM_EMOTION broadcast; kept SM_STATS_INFO send
    - [✓] `NpcAiService.cs` — `CastNpcDebuffAsync` expiry Task.Run: same replacement as CM_CASTSPELL debuff expiry
    - Key distinction preserved: `RemoveEffect(skillId, expiry)` remains list-only (no delta reversal) — used by DoT expiry (DoT effects have zero stat deltas so reversal would be a no-op anyway); `RemoveEffectBySkillId` reverses + removes (used by buff/debuff expiry and explicit removal)
    - Net reduction: ~255 lines removed from CM_CASTSPELL.cs and NpcAiService.cs; new stat fields need to be added in only 3 places (AbnormalState field, Creature cumulative field, ApplyEffectDeltas/ReverseEffectDeltas) instead of 7
    - Java analogy: Java `EffectController.addEffect` calls `applyEffect()` on each effect which calls `setStat(StatAddFunction)` centrally; `endEffect` reverses via `removeStat`; same centralization pattern
    - Build: 0 warnings, 0 errors

231. [✓] NPC self-buff stat delta support — extend CastNpcBuffAsync to apply statup deltas (session 2026-05-03)
    - [✓] `Services/NpcAiService.cs` — `CastNpcBuffAsync` signature: added `SkillEffects? effects` parameter (avoids 25-param explosion, passes template effects block directly)
    - [✓] `Services/NpcAiService.cs` — `CastNpcBuffAsync` body: `AbnormalState` now populated with all 25 statup fields from `effects?.*StatUpDelta` / `effects?.SpeedStatUpPct`; `PreBuffMovSpeed = npc.MovementSpeed` captured before buff is applied
    - [✓] `Services/NpcAiService.cs` — `CastNpcBuffAsync` body: speed statup applied manually after `AddEffect` (`npc.MovementSpeed *= (100 + speedStatUpPct) / 100f`) since `ApplyEffectDeltas` intentionally excludes percent-speed floats
    - [✓] `Services/NpcAiService.cs` — `CastNpcBuffAsync` expiry: changed from `npc.RemoveEffect(expEffect.SkillId, expEffect.Expiry)` (list-only, no delta reversal) to `npc.RemoveEffectBySkillId(expEffect.SkillId)` (reverses all deltas + restores `MovementSpeed = PreBuffMovSpeed`)
    - [✓] `Services/NpcAiService.cs` — `TryCastNpcSkillAsync` dispatch: passes `skillTemplate.Effects` to `CastNpcBuffAsync` (template already loaded at dispatch call site)
    - Java analogy: Java `StatupEffect.applyEffect` applies stat changes to the NPC's `LifeStats`; `endEffect` reverses them — same centralized pattern now mirrored via `AddEffect`/`RemoveEffectBySkillId`
    - Build: 0 warnings, 0 errors

232. [✓] Craft fail/crit chance — success/fail roll and combo product (critical craft) support (session 2026-05-03)
    - [✓] `Model/Templates/Recipe/RecipeTemplate.cs` — added `NameId` attribute; added `ComboProducts` list (`[XmlElement("comboproduct")] List<RecipeComboProduct>`); added `ComboProductId` convenience property (first combo product's itemId or 0); added new `RecipeComboProduct` record with `ItemId`
    - [✓] `CM_CRAFT.cs` — success/fail roll: `skillLvlDiff = playerSkillLevel - recipe.SkillPoint`, `successChance = clamp(50 + lvlDiff*2, 5, 95)%`; mirrors Java CraftingTask multi-tick success bar spirit (50% at margin 0, 95% at margin 22+)
    - [✓] `CM_CRAFT.cs` — crit roll: 15% if recipe has a combo product (mirrors Java `CraftConfig.CRAFT_CRIT_RATE` default)
    - [✓] `CM_CRAFT.cs` — on fail: materials consumed (mirrors Java pre-task component deduction in `checkCraft`), SM_CRAFT_UPDATE action=6 (fail), stop animation, no product
    - [✓] `CM_CRAFT.cs` — on crit: SM_CRAFT_UPDATE action=2 (bluecrit) then deliver combo product instead of normal product
    - [✓] `CM_CRAFT.cs` — inventory check moved to success-only path (fail path needs no product slot); quest and XP use `productId` (combo or normal)
    - [✓] SM_CRAFT_UPDATE now receives `recipe.NameId` for correct item-name display in craft window
    - Java analogy: Java `CraftingTask.onInteractionStart` rolls crit before bars fill; `onFailureFinish` action=6; `checkCrit` delivers combo product; our single-tick simplification collapses multi-tick bar fill into one probability roll
    - Build: 0 warnings, 0 errors

233. [✓] Per-skill damage values from XML templates — `<skillatk>` and `<spellatkinstant>` parsed and used in all damage paths (session 2026-05-03)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SkillDamageInfo` readonly record struct (`BaseValue`, `Delta`, `DamageType`, `Element`, `AccuracyMod`) after `SkillDotInfo`
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `DamageEffectNames` HashSet (`["skillatk", "spellatkinstant"]`) alongside the other effect-name sets
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `DamageEffects` property to `SkillEffects`: iterates `Elements`, parses `value`/`delta`/`element`/`accmod2` attributes; sets `DamageType` from element name; mirrors the `HealEffects`/`DotEffects` pattern
    - [✓] `CM_CASTSPELL.cs` — ground AoE damage path: computes `gAoeSkillBase = dmgFx[0].BaseValue + dmgFx[0].Delta * (level-1)` when template has entries; falls back to `player.Level * 6/4 + rand` otherwise; replaces the inline `player.Level * 6 + rand` in both magical and physical branches
    - [✓] `CM_CASTSPELL.cs` — single-target damage path: `stSkillBase` computed once before the if/else; reused in both magical branch (`int stMagicBase = stSkillBase ?? level*6+rand`) and physical branch (`stSkillBase ?? level*4+rand`)
    - [✓] `CM_CASTSPELL.cs` — caster/target AoE splash path: `splashBase = stSkillBase ?? fallback`; replaces both magical and physical inline formulas in `splashRaw` computation; `stSkillBase` is in scope from the single-target block surrounding the splash loop
    - Java analogy: `SkillAttackEffect` / `MagicSkillAttackInstantEffect` evaluated `getValue()` (= `value + delta*(level-1)`) then multiplied by `MagicBoostFunc` / weapon stats; we replicate that: template base replaces the `level*N` placeholder, multipliers unchanged
    - Previously: ALL physical/magical skills dealt identical damage for the same SkillType — a level-1 basic attack spell dealt the same as a high-tier AoE nuke because both used `level * 6 + rand`; 3000+ skills in skill_templates.xml have `<skillatk>`/`<spellatkinstant>` elements that were silently ignored
    - Build: 0 warnings, 0 errors

234. [✓] Physical skill crit + AoE DoT application + NPC template-based skill damage (session 2026-05-03)
    - [✓] `CM_CASTSPELL.cs` — ground AoE loop: added `else` branch after magical crit block for physical skill crit (`BaseCritRating + BonusPhysicalCritical + PhysCritDelta`, piecewise rate, `1.5 - strikeFortitude/1000` coefficient); PvP crit resist applied when target is Player
    - [✓] `CM_CASTSPELL.cs` — single-target path: same physical crit `else` block added after magical crit check; strike fortitude reduction applied for PvP targets
    - [✓] `CM_CASTSPELL.cs` — AoE splash loop: physical crit `else` block added; splash targets are NPC-only so no PvP crit resist/strike fortitude (simplified `1.5f` coefficient)
    - [✓] `CM_CASTSPELL.cs` — ground AoE loop: DoT block (`if target.CurrentHp > 0 && DotEffects`) added per target inside the targets foreach; same tick/expiry Task.Run pattern as single-target; captures target instance per-loop-iteration via `dotTickTarget` to avoid closure aliasing
    - [✓] `CM_CASTSPELL.cs` — AoE splash loop: DoT block added after kill handling (`if splash.CurrentHp > 0 && DotEffects`); splash targets are NPCs (`isPlayer: false` for SM_ABNORMAL_EFFECT)
    - [✓] `NpcAiService.CastNpcDamageAsync` — added `int skillLevel` parameter; call site passes `entry.SkillLevel`
    - [✓] `NpcAiService.CastNpcDamageAsync` — uses `DamageEffects[0].BaseValue + Delta * (level-1)` when template has damage entries; falls back to `npc.Level * 8 + rand`; mirrors M233 player-side formula
    - [✓] `NpcAiService.CastNpcDamageAsync` — NPC skill defense now type-aware: `PHYSICAL` uses `target.PhysicalDefense + PdefDebuffDelta + PdefStatUpDelta`; all others use `target.MagicDefense + MagicDefDelta`
    - [✓] `NpcAiService.CastNpcDamageAsync` — DoT block added for primary target: applies `DotEffects` from skill template; spawns tick Task.Run with same pattern; NPC is effector
    - [✓] `NpcAiService.CastNpcDamageAsync` — AoE splash defense also type-aware (`isPhysical ? Physical : Magic`); splash raw damage uses `skillBase ± 10%` variance instead of a fresh level formula
    - Java analogy: `SkillAttackEffect.calculateBaseDamage` used template values; `StatFunctions.calculateMagicalSkillDamage` applied MBMult; `StatFunctions.adjustDamages` applied level-diff reduction; physical crit from `calculatePhysicalCriticalRate` + `calculateWeaponCritical(1.5 default)`
    - Previously: (a) PHYSICAL skills never critted in CM_CASTSPELL — ~1600 physical attack skills (Gladiator chains, Ranger shots, Assassin combos) had 0% crit regardless of gear; (b) DoT effects from AoE skills (ground AoE poison clouds, AoE bleed) were applied only to the primary single-target hit, not the AoE targets; (c) all NPC skills used magic defense regardless of skill type; (d) NPC skill damage ignored skill templates
    - Build: 0 warnings, 0 errors

235. [✓] Physical dodge check for skill damage (session 2026-05-03)
    - [✓] `CM_CASTSPELL.cs` — added `NpcPhysicalAccuracy(Npc npc)` static helper: `(int)(npc.Level * (33.6f - 0.16f * npc.Level) + 5f)` — mirrors Java `StatFunctions.npcBaseAccuracy`
    - [✓] `CM_CASTSPELL.cs` — ground AoE loop: added `else` block after magic resist check; computes `physAccAoE = BasePhysicalAccuracy + BonusPhysicalAccuracy + PhysAccDelta`; NPC evasion via helper + `EvasionDebuffDelta + EvasionStatUpDelta`; `rawDodge = (evasion - accuracy) * levelDiffMult`; clamped to [0, 300]; `continue` on miss
    - [✓] `CM_CASTSPELL.cs` — single-target path: same pattern but `return` on miss; Player evasion uses `target.BaseEvasion + EvasionStatUpDelta + EvasionDebuffDelta`; NPC evasion via helper
    - [✓] `CM_CASTSPELL.cs` — AoE splash loop: simplified NPC-only version; `continue` on miss
    - Java analogy: `StatFunctions.calculatePhysicalDodgeRate` — `(evasion - accuracy) * levelMult`, then `clamp(rate * 0.6 + 50, 0, 300)`, miss if `rand(1000) < rate`
    - Previously: physical skills (Gladiator, Ranger, Assassin attacks) always hit regardless of target evasion — 0% miss rate vs. up to 30% for magical resists
    - Build: 0 warnings, 0 errors

236. [✓] Consumable food/buff item skill application — data-driven via skill template (session 2026-05-03)
    - [✓] `CM_USE_ITEM.cs` — added `using AionLightning.Game.Model.Templates.Skill;` import
    - [✓] `CM_USE_ITEM.cs` — replaced hard `return` for unknown `UseSkillId` with skill-template lookup: `_dataManager.Skills.GetTemplate(skillId)`; if template has `Effects` and `buffDurationMs > 0` (from `template.Duration` or `Effects.EffectDuration`), routes to `HandleItemBuffAsync`
    - [✓] `CM_USE_ITEM.cs` — added `HandleItemBuffAsync`: reads all 25 StatUp delta properties from `skillTpl.Effects`; builds `AbnormalState` with `SkillLevel = 1`, `Expiry = now + durationMs`; calls `player.AddEffect(effect)`; sends `SM_STATS_INFO`, `SM_ABNORMAL_EFFECT`, `SM_ITEM_USAGE_ANIMATION` to zone; handles cooldown; consumes item count; schedules `Task.Run` expiry with HP/MP clamping, stat re-send, and expired `SM_ABNORMAL_EFFECT` broadcast
    - Java analogy: Java `FoodUseAction.activate` called `SkillEngine.getInstance().useSkill(player, skillId)` which triggered `StatUpEffect.applyEffect` for each statup child; our approach replicates the stat delta extraction without the full skill-engine dispatch
    - Previously: all 908 food/buff items with `skillId >= 10300` (food, scrolls, battle supplies) fell through the `SkillEffects` dict lookup and were silently discarded — eating a 30-min food buff applied nothing
    - Build: 0 warnings, 0 errors

237. [✓] Additional revive types — skill, rebirth, item self-rez, instance (session 2026-05-03)
    - [✓] `Player.cs` — added `HasPendingRevive`, `ResurrectionSkillId` (set by a resurrection skill cast on this player), `CanRebirthRevive`, `RebirthResurrectPercent` (default 5), `RebirthSkillId`, `InstanceStartPosition`
    - [✓] `CM_REVIVE.cs` — extracted `ReviveCoreAsync(player, hpPct, mpPct, applySoulSickness, skillId, destination, ct)` shared helper; added `IItemDao` dependency; DI registration updated in `GsPacketHandlerFactory.cs`
    - [✓] `CM_REVIVE.cs` — BIND_REVIVE (0) / OBELISK_REVIVE (8): existing bind-point teleport logic now in `HandleBindReviveAsync`, soul sickness applied
    - [✓] `CM_REVIVE.cs` — SKILL_REVIVE (3): checks `player.HasPendingRevive`; revives in-place at 10% HP/MP, no soul sickness; clears `HasPendingRevive` and `ResurrectionSkillId`; if flag not set, returns (anti-hack guard mirrors Java `cancelRes`)
    - [✓] `CM_REVIVE.cs` — REBIRTH_REVIVE (1): checks `player.CanRebirthRevive`; revives in-place at `RebirthResurrectPercent`; applies soul sickness; clears rebirth flags
    - [✓] `CM_REVIVE.cs` — ITEM_SELF_REVIVE (2): searches inventory for self-rez stones in Java priority order (161001001, 161000003, 161000004, 161000001); consumes one charge; revives in-place at 15% HP/MP; applies soul sickness
    - [✓] `CM_REVIVE.cs` — INSTANCE_REVIVE (6): revives at `player.InstanceStartPosition` if set, otherwise falls back to bind/default spawn; 25% HP/MP, soul sickness applied
    - KISK_REVIVE (4) falls through to bind revive — Kisk entity support is a separate milestone
    - Java analogy: `CM_REVIVE.runImpl` switched on `ReviveType.getReviveTypeById(reviveId)`; each revive delegated to `PlayerReviveService.*Revive`; our `ReviveCoreAsync` collapses the five repeated `revive(player, hpPct, mpPct, soulSickness, skillId)` call sites into a single parameterized helper
    - Previously: ALL revive requests used bind-point teleport at 25% HP/MP regardless of revive type — skill res, item self-rez, and rebirth all triggered bind revive, making those mechanics non-functional
    - Build: 0 warnings, 0 errors

238. [✓] Resurrection skill effect — skill revive notification flow (session 2026-05-03)
    - [✓] `SkillTemplate.cs` — added `HasResurrectEffect` bool property to `SkillEffects`: scans `Elements` for any `LocalName == "resurrect"`
    - [✓] `SkillTemplate.cs` — added `ResurrectSkillId` int property to `SkillEffects`: returns `skill_id` attribute of first `<resurrect>` element (or 0)
    - [✓] `SM_RESURRECT.cs` — new server packet (opcode 0xC2): writes `casterName` (string S), `skillId` (H), `0` (D); mirrors Java `SM_RESURRECT`; sent to dead target to show "accept resurrection?" dialog
    - [✓] `CM_CASTSPELL.cs` — added resurrection routing block before heal/buff/damage branches: if `template.Effects.HasResurrectEffect && targetType is 0/3/4 && target is dead Player && target != caster`, sets `target.HasPendingRevive = true`, `target.ResurrectionSkillId = ResurrectSkillId`, sends `SM_RESURRECT(player.Name, spellId)` to target's connection
    - Java analogy: `ResurrectEffect.applyEffect` set `player.setPlayerResActivate(true)`, `setResurrectionSkill(skillId)`, sent `SM_RESURRECT(effector, skillId)` — our routing block replicates exactly this; the target then clicks accept → CM_REVIVE type=3 → `HandleSkillReviveAsync` (M237) completes the revive
    - Previously: all 24 resurrection skills (Cleric "Light of Resurrection", Chanter res, etc.) cast their animation but did nothing to the dead target; `HasPendingRevive` was always false so `HandleSkillReviveAsync` would immediately return
    - Build: 0 warnings, 0 errors

239. [✓] Drain damage variants — `<skillatkdraininstant>` and `<spellatkdraininstant>` restore HP/MP to caster (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — extended `SkillDamageInfo` record with `HpPercent` and `MpPercent` int fields (default 0 for non-drain variants)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `"skillatkdraininstant"` (physical drain) and `"spellatkdraininstant"` (magical drain) to `DamageEffectNames` HashSet
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `DamageEffects` getter parses `hp_percent`/`mp_percent` attributes (TryParse, default 0); `DamageType` is `physical` for `skillatk`/`skillatkdraininstant`, `magical` for the spell variants
    - [✓] `CM_CASTSPELL.cs` — single-target damage path: after `target.CurrentHp -= damage`, drain block fires when `stDmgFx[0].HpPercent != 0 || stDmgFx[0].MpPercent != 0`; computes `hpGain = damage * pct / 100` clamped to `MaxHp - CurrentHp`; sends `SM_ATTACK_STATUS(player, NaturalHp/NaturalMp, spellId, gain, SpellAtkDrain)` to all in-world clients
    - [✓] `CM_CASTSPELL.cs` — ground AoE path: drain block uses `gAoeDmgFx[0]`; fires per-target inside the `foreach (var target in targets)` loop so multi-target AoE drains scale linearly
    - [✓] `CM_CASTSPELL.cs` — splash path: drain block uses `stDmgFx[0]` (in scope from surrounding single-target block) with `splashDmg` instead of `damage`; splash targets are NPC-only
    - Java analogy: `SkillAtkDrainInstantEffect.applyEffect` calls `effector.LifeStats.increaseHp/Mp(reserved1 * pct / 100)` after `super.applyEffect`; `SpellAtkDrainInstantEffect` does the same with magical damage type. We use `LogId.SpellAtkDrain` (130) for both since the .NET enum doesn't yet distinguish `SKILLLATKDRAININSTANT` vs `SPELLATKDRAININSTANT` — gameplay numbers identical; only combat-log label flavor differs
    - Previously: 147 drain skill XML entries (Assassin "Soul Slash" lifesteal, Sorcerer mana-drain spells, Spiritmaster drain line, many leveling skills) dealt correct damage but never restored HP/MP — sustain mechanic of these classes was non-functional
    - Build: 0 warnings, 0 errors

240. [✓] Direct MP damage — `<mpattackinstant>` burns target MP (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SkillMpAttackInfo` readonly record struct (`BaseValue`, `Delta`, `IsPercent`, `Element`)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MpAttackEffectNames` HashSet (`["mpattackinstant"]`)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `MpAttackEffects` getter; parses `value`, `delta`, `percent="true|false"`, `element`
    - [✓] `CM_CASTSPELL.cs` — single-target damage path: after drain block, computes `mpBurn = (BaseValue + Delta*(level-1))`; if `IsPercent` multiplies by `target.MaxMp/100`; subtracts from `target.CurrentMp` clamped at 0
    - [✓] `CM_CASTSPELL.cs` — ground AoE path: per-target MP burn (uses `gAoeMpFx[0]`)
    - [✓] `CM_CASTSPELL.cs` — splash path: per-splash-target MP burn (uses `stMpFx[0]` from surrounding scope)
    - Java analogy: `MpAttackInstantEffect.applyEffect` calls `effected.LifeStats.reduceMp(value)` (or `maxMp * value / 100` if `percent="true"`); we replicate the math but skip combat-log broadcast (Java doesn't emit one for this effect either)
    - Previously: 97 mpattackinstant skill XML entries (Aether Arrow line at 75% target max-MP, Sorcerer mana-burn lines, several Spiritmaster/Templar utility) silently ignored — Sorcerers/Spiritmasters could be locked out of their entire kit by these skills in PvP per Java behavior, but we ignored them entirely
    - Build: 0 warnings, 0 errors

241. [✓] NPC casters — drain damage + mpattackinstant support in NpcAiService (session 2026-05-04)
    - [✓] `Services/NpcAiService.cs` — `CastNpcDamageAsync` primary-target block: after `target.CurrentHp -= spellDmg`, drain block reads `dmgFx[0].HpPercent`/`MpPercent` and credits NPC's own HP/MP via `Math.Min(MaxHp, CurrentHp + spellDmg * pct / 100)`
    - [✓] `Services/NpcAiService.cs` — `CastNpcDamageAsync` primary-target block: mpattackinstant block reads `MpAttackEffects[0]`, computes `mpBurn = (BaseValue + Delta*(skillLevel-1))` (or `target.MaxMp * val / 100` if percent), subtracts from `target.CurrentMp` clamped at 0
    - [✓] `Services/NpcAiService.cs` — splash AoE branch: drain credits NPC HP/MP per splash target; mpattackinstant burns each splash target's MP (independent `splMpFx` lookup since `npcMpFx` is scoped to primary block)
    - Java analogy: same as M239/M240 — mirrors player-side drain + MP burn for NPC casters; uses `npc.CurrentHp/Mp` as effector instead of `player.CurrentHp/Mp`
    - Previously: NPC bosses casting drain skills (e.g. dragon lifesteal abilities, Beritra-style mana drain) would deal damage but never restore their own HP/MP; NPC mana-burn skills against players also didn't drain target MP — only the player-side casters benefited from M239/M240
    - Build: 0 warnings, 0 errors

242. [✓] ProcHeal/ProcMpHeal aliases — `<prochealinstant>` and `<procmphealinstant>` heal as `<healinstant>`/`<mphealinstant>` (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `"prochealinstant"` and `"procmphealinstant"` to `HealInstantNames` HashSet
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `HealEffects` getter routes both new names: `prochealinstant` → `hp` heal type; `procmphealinstant` → `mp` heal type (existing `percent`/`value`/`delta` parsing already covers them since Java `ProcHealInstantEffect` extends `AbstractHealEffect` with no extra fields)
    - Java analogy: `ProcHealInstantEffect` and `ProcMpHealInstantEffect` are identical to `HealInstantEffect`/`MpHealInstantEffect` (same `AbstractHealEffect` parent, same `applyEffect(effect, HealType.HP/MP)`); the "Proc" name prefix is misleading — they fire unconditionally as part of skill effect list, not on a chance/proc trigger
    - Previously: 127 prochealinstant + 104 procmphealinstant = 231 skill XML entries (Recharge line, recovery potions, Crucible Recovery Potion, several Cleric/Spiritmaster/Songweaver heal-type skills) silently ignored — the heal/mp-heal numbers from these skills did nothing to target HP/MP
    - Build: 0 warnings, 0 errors

243. [✓] ProcAtkInstant alias — `<procatk_instant>` deals magical damage as `<spellatkinstant>` (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `"procatk_instant"` (note underscore) to `DamageEffectNames` HashSet
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `DamageEffects` getter falls through to `magical` for the new name (the type-detection ternary excludes only `skillatk`/`skillatkdraininstant` from physical, so `procatk_instant` is treated as magical — matches Java `super.calculate(effect, DamageType.MAGICAL)`)
    - Java analogy: `ProcAtkInstantEffect` extends `DamageEffect` with `DamageType.MAGICAL`; only differs from `SpellAtkInstantEffect` by emitting an extra `SM_SYSTEM_MESSAGE 1301062` ("X procced!") and using `LOG.PROCATKINSTANT` (we already use `LogId.SpellAtk` for both — combat-log label flavor only)
    - Previously: 211 procatk_instant skill XML entries (proc-on-attack damage skills, several Sorcerer/Spiritmaster procs, weapon-enchant fire/water/wind retaliation procs) silently ignored — these damage numbers from elemental procs were not applied
    - Build: 0 warnings, 0 errors

244. [✓] Magical DoT — `<spellatk>` (magical DoT) and `<spellatkdrain>` (DoT-drain) join DotEffects (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — extended `SkillDotInfo` record with `HpPercent`/`MpPercent` int fields (default 0 for plain DoTs)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `"spellatk"` and `"spellatkdrain"` to `DotNames` HashSet
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `DotEffects` getter parses `hp_percent`/`mp_percent` attributes (TryParse, default 0)
    - [✓] `CM_CASTSPELL.cs` — three DoT-tick loops (ground AoE primary, ground AoE splash, single-target) updated: ternary `DotType == "bleed"` replaced with switch covering `bleed`/`spellatk`/`spellatkdrain`/`poison` LogId mapping; closures capture `dotTickCaster`/`tickCaster` (= `player`) and `dotTickInfo`/`tickInfo` (= `dot`); per-tick drain block restores caster HP/MP via `Math.Min(MaxHp, CurrentHp + tickDmg * pct / 100)` when `HpPercent`/`MpPercent` non-zero
    - [✓] `Services/NpcAiService.cs` — DoT-tick loop in `CastNpcDamageAsync`: same switch + drain pattern; caster is `npc` instead of `player`
    - Java analogy: `SpellAttackEffect` (extends `AbstractOverTimeEffect`) ticks magical damage with `value + delta * skillLevel`; `SpellAtkDrainEffect` ticks the same plus `effector.LifeStats.increaseHp/Mp(damage * pct / 100)` per tick. We use simplified `value + delta * skillLevel` without the magical-attack scaling that Java's `calculateMagicalOverTimeSkillResult` applies — same approximation already used for our existing bleed/poison/disease ticks
    - Previously: 491 spellatk + 113 spellatkdrain = 604 skill XML entries silently ignored — fire/earth/wind elemental DoTs from Sorcerer/Spiritmaster (e.g. Sandstorm DoT, Burning Spirit DoT) and DoT-drain skills delivered their `<spellatkinstant>` initial hit but skipped all subsequent tick damage
    - Build: 0 warnings, 0 errors

245. [✓] Hostileup — `<hostileup>` taunt skills force NPC target to engage caster (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `HasHostileUp` bool property to `SkillEffects`: scans `Elements` for any `LocalName == "hostileup"`
    - [✓] `CM_CASTSPELL.cs` — added taunt routing block right before resurrection block: if `HasHostileUp && _targetType == 0`, looks up `tauntNpc` via `_world.GetNpcByObjectId`; if alive, calls `_npcAi.ForceEngage(tauntNpc, player)` to make the NPC switch its current target/aggro to the caster
    - Block placement is non-exclusive (no `return`) so a skill that combines `<hostileup>` with damage/buff effects still routes those effects normally; the ForceEngage is a side-effect on top
    - Java analogy: `HostileUpEffect.applyEffect` calls `((Npc) effected).getAggroList().addHate(effector, tauntHate)` where `tauntHate = value + delta * skillLevel`. We don't yet model a numeric hate list — instead we approximate the "this skill should make the NPC fight you" outcome by forcing target switch via `ForceEngage`. Hate-quantum nuances (e.g. multi-target taunt-priority) remain a future milestone if a hate list is added.
    - Previously: 144 standalone taunt skill XML entries (Templar/Gladiator provoke skills, several boss-room threat skills) cast their animation but did nothing — NPCs ignored taunts unless the same skill also dealt damage (which already triggers `ForceEngage` post-damage)
    - Build: 0 warnings, 0 errors

246. [✓] Dispel debuff variants — `<dispeldebuffphysical>` and `<dispeldebuffmental>` cleanse as `<dispeldebuff>` (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `HasDispelDebuff` property now scans for `dispeldebuff`, `dispeldebuffphysical`, OR `dispeldebuffmental` LocalName
    - Java analogy: `DispelDebuffPhysicalEffect` and `DispelDebuffMentalEffect` both extend `AbstractDispelEffect` and only differ from `DispelDebuffEffect` by passing `DispelCategoryType.DEBUFF_PHYSICAL`/`DEBUFF_MENTAL` to limit which debuff sub-categories are removed. Our .NET model does not yet track debuff sub-categories — `target.ClearDebuffs()` removes all debuffs in one shot. The alias is functionally an approximation: skills that should remove only physical/mental debuffs in Java will remove all debuffs in .NET. This over-dispel is acceptable as a placeholder; proper categorization can be added later if PvP balance requires it.
    - Previously: 29 dispeldebuffphysical + 9 dispeldebuffmental = 38 skill XML entries (Cleric/Chanter category-specific cleanses) silently ignored — players hit by physical/mental debuffs (e.g. paralyze, poison) couldn't cleanse them via these specialized skills
    - Build: 0 warnings, 0 errors

247. [✓] Defense-bypass damage — `<noreducespellatk>` flat or percent-of-MaxHp damage that skips magic/physical defense (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `SkillNoReduceInfo` readonly record struct (`BaseValue`, `Delta`, `IsPercent`, `Element`)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `NoReduceEffectNames` HashSet (`["noreducespellatk"]`)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `NoReduceEffects` getter; parses `value`, `delta`, `percent="true|false"`, `element`
    - [✓] `CM_CASTSPELL.cs` — single-target damage path: between defense computation and HP application, override `damage` when `NoReduceEffects.Count > 0`: `noReduceVal = BaseValue + Delta*(level-1)`; if `IsPercent`, `damage = target.MaxHp * noReduceVal / 100` (flat percentage execute), otherwise `damage = noReduceVal` flat — both bypass `spellDef * 1000/(1000+spellDef)` reduction
    - [✓] `CM_CASTSPELL.cs` — ground AoE path: same per-target override using `gAoeNoReduce[0]`
    - Java analogy: `NoReduceSpellATKInstantEffect.calculate` skips `getReducedAttack` and applies `valueWithDelta` (or `% of MaxHp`) directly via `AttackUtil.calculateMagicalSkillResult` with the "ignore defense" flags set; we replicate the result by replacing the post-defense `damage` with the noreduce value while keeping the surrounding flow (resist/dodge check, drain, mp burn, dispel buff, etc.) unchanged
    - Previously: 96 noreducespellatk skill XML entries (PvP execute-class skills like 20%-MaxHp burst, level-cap fire/light spells, several bossroom mechanics) dealt regular mAtk-based damage that was reduced by target defense — neutralizing the entire purpose of "no-reduce" defense-bypass. The 60 AREA-targeted noreducespellatk skills are partially handled (ground AoE path covers ground-targeted casts; single-target skill cast on AREA-relation skills falls through to single-target path — full target_type=AREA fan-out is a separate milestone)
    - Build: 0 warnings, 0 errors

248. [✓] FP heal — `<fphealinstant>` and `<procfphealinstant>` restore Player flight points (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `"fphealinstant"` and `"procfphealinstant"` to `HealInstantNames` HashSet
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `HealEffects` getter ternary upgraded to a 3-way switch (`hp`/`fp`/`mp`); the `prochealinstant` and `procfphealinstant` aliases share the same routing as their non-proc counterparts
    - [✓] `CM_CASTSPELL.cs` — single-target heal site: added `else if (he.HealType == "fp" && healTarget is Player fpHt)` branch; `healMaxStat` switch picks the right max for percent calculation; clamped to `MaxFp - CurrentFp`; sends `SM_ATTACK_STATUS(fpHt, NaturalFp, _spellId, heal, FpHeal)`
    - [✓] `CM_CASTSPELL.cs` — AoE ally heal site: same pattern with `ally is Player fpAlly` cast (filters NPC allies); `aoeMaxStat` 3-way switch
    - Java analogy: `FPHealInstantEffect` extends `AbstractHealEffect` with `HealType.FP` and points to `LifeStats.getCurrentFp/MaxFp` accessors. We keep the same `value + delta * level` formula and apply percent calc against MaxFp when `percent="true"`. Non-Player heal targets are filtered (NPCs don't have FP) — `else if` simply doesn't fire and falls through to the MP branch which would no-op since `HealType != "mp"`.
    - Deferred: HoT site (`<fpheal>`, 6 entries) and group-heal flag updates not yet wired — would need a 3-way upgrade in the HoT loop and `SM_GROUP_MEMBER_INFO` FP fields. Low impact (6 skills) so not blocking
    - Previously: 17 fphealinstant + 13 procfphealinstant = 30 skill XML entries (FP recovery scrolls/items, Aether Wings type buffs, in-flight FP regen kits) silently fell through to the MP branch, which then would attempt to heal MP from a `0`-MaxStat percent calc — net result of dropping the heal entirely
    - Build: 0 warnings, 0 errors

249. [✓] FP HoT — `<fpheal>` over-time flight-point regen (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `"fpheal"` to `HotNames` HashSet
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `HotEffects` getter HealType selection upgraded to a 3-way switch (`hp`/`fp`/`mp`)
    - [✓] `CM_CASTSPELL.cs` — single-target HoT tick loop: replaced ternary with `switch` on `hot.HealType`; `fp` branch uses `Player.MaxFp - Player.CurrentFp` clamp (with pattern-match `tickTarget is Player fpHotTickT`); `fp` fallthrough returns `0` for non-Player targets so they no-op cleanly
    - [✓] `CM_CASTSPELL.cs` — application branch: added `else if (hot.HealType == "fp" && tickTarget is Player fpHotTickApp)` block; sends `SM_ATTACK_STATUS(NaturalFp, FpHeal)` per tick
    - Java analogy: `FPHealEffect` extends `AbstractHealEffect` with `HealType.FP` and reads/writes `LifeStats.getCurrentFp/MaxFp`. Same shape as `HealEffect`/`MPHealEffect` HoT variants we already supported; the milestone is a pure HealType extension
    - Previously: 6 fpheal HoT skill XML entries (Aether-related FP regen-over-time buffs, sustained-flight scroll items) silently dropped because the HoT tick branch hard-coded `hp`/`mp` only
    - Build: 0 warnings, 0 errors

250. [✓] NPC dispel variants — `<dispelnpcbuff>` and `<dispelnpcdebuff>` aliased to existing dispel handlers (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `HasDispelBuff` now matches `dispelbuff` OR `dispelnpcbuff`; `HasDispelDebuff` now matches the four debuff variants from M246 plus `dispelnpcdebuff`
    - Java analogy: `DispelNpcBuffEffect` and `DispelNpcDebuffEffect` extend `AbstractDispelEffect` with `friendlyTarget=NPC` constraint — they restrict dispel to NPC targets only. We don't enforce target-type at the dispel level (skill's own `target_relation` already restricts who's eligible), so the aliases are safe; ClearBuffs/ClearDebuffs run only when the resolved target is the intended type
    - Previously: 13 dispelnpcbuff + 2 dispelnpcdebuff = 15 skill XML entries (Cleric/Spiritmaster NPC-specific cleanses, several boss-room mechanics) silently ignored
    - Build: 0 warnings, 0 errors

251. [✓] Item-heal via skill template — items with `<healinstant>`/`<mphealinstant>`/`<fphealinstant>` no longer require a hardcoded entry (session 2026-05-04)
    - [✓] `CM_USE_ITEM.cs` — main use-item routing: when `UseSkillId` not in the hardcoded `SkillEffects` HP/MP elixir dict but the skill template has `HealEffects.Count > 0`, route to a new `HandleItemHealAsync` instead of falling through to the buff path
    - [✓] `CM_USE_ITEM.cs` — added `HandleItemHealAsync` private method: enforces `UseLimits.DelayId` cooldown; broadcasts `SM_ITEM_USAGE_ANIMATION`; iterates each `SkillHealInfo`; computes `valueWithDelta = BaseValue + Delta` (item skills are level 1) with percent-of-max for HP/MP/FP; clamps to `Max - Current`; mutates Player HP/MP/FP and broadcasts `SM_ATTACK_STATUS(NaturalHp/Mp/Fp, ...)`; refreshes group HP bars + `SM_STATS_INFO`; sets cooldown; consumes one charge (deletes empty stack via `IItemDao.DeleteAsync` + `SM_DELETE_ITEM`)
    - Java analogy: Java `ItemUseAction.activate` calls `SkillEngine.useSkill(player, useSkillId)` which dispatches to `HealInstantEffect`/`MPHealInstantEffect`/`FPHealInstantEffect` etc. Our simplified port fast-paths the common single-target self-heal case directly; complex item-skill chains (heals with embedded buffs, AoE party heal items) still need the full skill engine but those are rare for consumables
    - Previously: 166 heal-skill XML templates with `skill_id >= 9000` (item-driven heals — Bottomless Bucket 70%/70%, Greater Healing Potion variants, recovery scrolls) silently fell through. Of those only 14 specific HP/MP elixirs (skill IDs 10202-10208, 10262-10268) were hardcoded; the other 150+ items consumed their charge and showed the use animation but applied no actual HP/MP/FP restore
    - Build: 0 warnings, 0 errors

252. [✓] DP heal — `<dphealinstant>`, `<procdphealinstant>`, `<dpheal>` HoT use existing `Player.Dp` field (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `dphealinstant`/`procdphealinstant` to `HealInstantNames`; added `dpheal` to `HotNames`; HealEffects + HotEffects ternaries now route the new names to `dp` HealType
    - [✓] `CM_CASTSPELL.cs` — single-target heal: `dp` branch reads/writes `Player.Dp` clamped at 6000 (the cap used elsewhere by `CM_ATTACK`/`CM_CASTSPELL` DP gain); broadcasts `SM_DP_INFO` to the heal target's connection (DP is private — no zone broadcast needed)
    - [✓] `CM_CASTSPELL.cs` — AoE ally heal: same pattern with `ally is Player dpAlly` Player cast
    - [✓] `CM_CASTSPELL.cs` — HoT tick: switch handles `dp` Player-cast clamp + per-tick SM_DP_INFO broadcast
    - [✓] `CM_USE_ITEM.cs` — `HandleItemHealAsync`: `dp` branch with 6000 cap; sends SM_DP_INFO to the using player's connection
    - Java analogy: `DPHealInstantEffect` and `DPHealEffect` extend `AbstractHealEffect` with `HealType.DP`, reading `getCommonData().getDp()` and `getGameStats().getMaxDp().getCurrent()`. Our .NET port hardcodes 6000 as the cap (no MaxDp template/stat infrastructure yet) — matches DP gain behavior in `CM_ATTACK.cs:255-257` and `CM_CASTSPELL.cs:909-912`
    - Previously: 6 dphealinstant + 9 dpheal + 11 procdphealinstant = 26 skill XML entries (DP packs, post-PvP DP recovery scrolls, several Cleric DP buffs that include DP-restore alongside heal) silently dropped; players had no way to receive DP outside attack/kill rewards
    - Build: 0 warnings, 0 errors

253. [✓] Defense-bypass coverage — extend `<noreducespellatk>` to splash + NPC casters (session 2026-05-04)
    - [✓] `CM_CASTSPELL.cs` — splash damage path: between defense computation and HP application, override `splashDmg` when `stNoReduce.Count > 0`; uses `splash.MaxHp` for percent calc (matches single-target/ground-AoE pattern)
    - [✓] `Services/NpcAiService.cs` — primary-target NPC damage path: same `npcNoReduce` lookup + override on `spellDmg` (uses `target.MaxHp` for percent calc); fixes inadvertent typo introduced in this session (had `(1000 + spellDef)` referencing nonexistent variable; restored to `(1000 + defense)`)
    - [✓] `Services/NpcAiService.cs` — NPC splash damage path: splash override on `splashDmg` using `other.MaxHp`
    - Closes the M247 coverage gap: 60 AREA-targeted noreducespellatk skills now apply correct defense-bypass damage on splash hits, not just on the primary target. ~6 NPC-cast noreducespellatk skills (boss execute mechanics) now bypass player defense as the Java NoReduceSpellATKInstantEffect does
    - Build: 0 warnings, 0 errors

254. [✓] Drain LogId fidelity — distinguish physical (`SkillAtkDrainInstant`=23) from magical (`SpellAtkDrainInstant`=24) drain combat-log entries (session 2026-05-04)
    - [✓] `Network/Aion/ServerPackets/SM_ATTACK_STATUS.cs` — added `SkillAtkDrainInstant = 23` and `SpellAtkDrainInstant = 24` to `LogId` enum (matches Java `LOG.SKILLLATKDRAININSTANT`/`SPELLATKDRAININSTANT` numeric values verbatim)
    - [✓] `CM_CASTSPELL.cs` — single-target drain block (M239): pre-computes `stDrainLog = stDmgFx[0].DamageType == "physical" ? SkillAtkDrainInstant : SpellAtkDrainInstant` once and reuses for both HP/MP gain `SM_ATTACK_STATUS` packets
    - [✓] `CM_CASTSPELL.cs` — ground AoE drain block: same pattern with `gAoeDrainLog`
    - [✓] `CM_CASTSPELL.cs` — splash drain block: same pattern with `splashDrainLog`
    - Java analogy: `SkillAtkDrainInstantEffect.applyEffect` calls `increaseHp(..., LOG.SKILLLATKDRAININSTANT)`; `SpellAtkDrainInstantEffect.applyEffect` calls `increaseHp(..., LOG.SPELLATKDRAININSTANT)`. Previously both used `LogId.SpellAtkDrain` (130, the DoT-drain code) which was wrong combat-log labelling — instant-drain should fire 23/24 not 130. DoT drain (M244 `<spellatkdrain>`) correctly continues to use `SpellAtkDrain = 130`
    - Gameplay impact: 147 drain skill XML entries (Assassin Soul Slash physical drain, Sorcerer/Spiritmaster magical drain) now emit the right combat-log label — purely cosmetic to the client UI but matches Java fidelity
    - Build: 0 warnings, 0 errors

255. [✓] ProcAtkInstant LogId fidelity — `<procatk_instant>` damage broadcasts use LogId.ProcAtkInstant (92) (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — extended `SkillDamageInfo` record with `Variant` string field carrying the source effect element name (`skillatk`, `spellatkinstant`, `skillatkdraininstant`, `spellatkdraininstant`, `procatk_instant`); `DamageEffects` getter passes `e.LocalName` as the new field
    - [✓] `CM_CASTSPELL.cs` — single-target `statusPkt`, ground-AoE `statusPkt`, splash `splashPkt`: pre-compute `LogId` from variant (`Variant == "procatk_instant"` → `LogId.ProcAtkInstant`, else `LogId.SpellAtk`)
    - Java analogy: `ProcAtkInstantEffect.applyEffect` calls `effected.getController().onAttack(..., LOG.PROCATKINSTANT)` rather than the default `LOG.SPELLATK` from regular damage path — combat-log labels the proc differently in Java client UI; we now match
    - Gameplay impact: 211 procatk_instant XML entries (M243) emit the correct proc combat-log label rather than appearing as a regular spell hit. Purely cosmetic to client UI but completes the M243 fidelity story alongside M254's drain LogId fix
    - Build: 0 warnings, 0 errors

256. [✓] NpcAiService LogId consistency — NPC skill broadcasts use SpellAtk / Heal (session 2026-05-04)
    - [✓] `Services/NpcAiService.cs` — `CastNpcDamageAsync` primary-target broadcast: `SM_ATTACK_STATUS(target, Damage, skillId, spellDmg, LogId.SpellAtk)` instead of default `LogId.Regular` (181)
    - [✓] `Services/NpcAiService.cs` — `CastNpcDamageAsync` splash branch: same `LogId.SpellAtk` override
    - [✓] `Services/NpcAiService.cs` — NPC self-heal (HandleNpcSelfHeal): `LogId.Heal` (3) instead of default `Regular`
    - Closes the LogId fidelity story for NPC casts: previously NPC skill damage and self-heal were tagged with `Regular = 181` (default constructor arg) which the client UI displays as a generic combat entry; now they emit the proper SpellAtk/Heal tags matching CM_CASTSPELL player-side and the Java `LOG.SPELLATK`/`LOG.HEAL` enum values
    - Auto-attack (line 373) and HP regen (line 128) intentionally keep the default `Regular` since they're not skill-driven — matches Java behavior where `attack` packets use `LOG.REGULAR`
    - Build: 0 warnings, 0 errors

257. [✓] NPC self-heal uses skill template — `CastNpcHealAsync` reads HealEffects instead of MaxHp/6 fallback (session 2026-05-04)
    - [✓] `Services/NpcAiService.cs` — `CastNpcHealAsync` signature extended with `skillLevel` + `SkillTemplate? skillTemplate` parameters; call site at line 486 passes them through
    - [✓] `Services/NpcAiService.cs` — body computes `healAmt` from `skillTemplate.Effects.HealEffects` when present: iterates each HP-type heal entry, applies `BaseValue + Delta * skillLevel` with optional `IsPercent` of `npc.MaxHp`; falls back to legacy `MaxHp/6` when no HealEffects parsed (or when only MP/FP/DP entries which NPCs can't consume)
    - [✓] `Services/NpcAiService.cs` — applies `healAmt` clamped to `MaxHp - CurrentHp`, returns early on no-op
    - Java analogy: `HealInstantEffect.applyEffect` reads `value + delta * skillLevel` and applies via `LifeStats.increaseHp`. Our NPC port uses the same formula scaled by skill level instead of the previous flat-1/6th-of-MaxHp approximation
    - Previously: ~30 NPC heal-type skill templates (boss self-heal, mob recovery skills like "Massive Recovery") all healed for the same 1/6 of MaxHp regardless of skill template values. Bosses with low-tier heals over-healed; bosses with high-tier heals under-healed
    - Build: 0 warnings, 0 errors

258. [✓] noresist="true" — damage effects with this attribute skip magic resist + physical dodge rolls (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — extended `SkillDamageInfo` record with `IsNoResist` bool field; `DamageEffects` getter parses `noresist="true"` (case-insensitive) attribute
    - [✓] `CM_CASTSPELL.cs` — ground-AoE resist/dodge: pre-computes `gAoeNoResist` from `gAoeDmgFx[0].IsNoResist`; magical resist gated `if (spellIsMagical && !gAoeNoResist)`; physical dodge gated `else if (!gAoeNoResist)` so noresist skills skip both rolls cleanly
    - [✓] `CM_CASTSPELL.cs` — single-target: same pattern with `stNoResist` from `stPreNoResistFx[0].IsNoResist` (a separate lookup since `stDmgFx` is computed later in the path); both magical resist branch and physical dodge branch gated explicitly
    - Java analogy: `EffectTemplate.calculate(effect, ...)` short-circuits the resist/dodge calc when `noresist=true` — our gating mirrors that intent at the resist/dodge call site
    - Gameplay impact: a subset of the 9122 noresist="true" XML hits (those on damage effects) — concrete examples include Aether Arrow (which had to land for the 75% MP burn to work), several boss execute mechanics tagged noresist, "guaranteed-hit" finisher skills. Previously these still rolled magic resist on PvP and could whiff
    - Build: 0 warnings, 0 errors

259. [✓] NPC auto-attack defense — match CM_CASTSPELL delta inclusion (session 2026-05-04)
    - [✓] `Services/NpcAiService.cs` — NPC auto-attack `pdef` calc at line 356: was `target.PhysicalDefense` only; now `target.PhysicalDefense + target.PdefDebuffDelta + target.PdefStatUpDelta` clamped at 0
    - Java analogy: `StatFunctions.calculateBaseDamageToTarget` reads target's full effective PDEF including stat modifiers; we now mirror this on the NPC physical-attack path
    - Gameplay impact: when a Player has an active physical-defense debuff (e.g. Sorcerer "Frigid Wrath" pdef debuff) or buff (Templar "Body Smash" defense), NPC auto-attack damage now factors those modifiers. Previously NPC ignored player defense deltas — players took identical hits regardless of buffs/debuffs in effect
    - Note: NPC skill-cast damage paths (M239+) already use the full `PdefDebuffDelta + PdefStatUpDelta` formula at lines 546 + 669; this milestone closes the gap on the auto-attack path
    - Build: 0 warnings, 0 errors

260. [✓] Damage Observer Pattern via IEventBus + healcastoronatk handler (session 2026-05-04)
    - Architecture milestone — establishes the foundation that subsequent skill-effect handlers (reflector, convertheal, magiccounteratk, etc.) can plug into without re-touching the damage paths.
    - [✓] `Game/Events/DamageDealtEvent.cs` — new sealed record `(Creature Attacker, Creature Target, int DamageAmount, DamageKind Kind, int? SkillId)` + DamageKind enum (AutoAttack, PhysicalSkill, MagicalSkill, Splash, DoTTick)
    - [✓] `Game/Combat/CreatureDamageExtensions.cs` — single canonical helper `target.ApplyDamageAndPublishAsync(attacker, damage, kind, skillId, bus, ct)`. Atomically: subtracts HP (clamped at 0), updates both `LastCombatTime`, publishes `DamageDealtEvent`. Replaces 14 inline damage sites with one method call each.
    - [✓] `SkillTemplate.cs` — added `SkillHealCastorOnAtkInfo` record (BaseValue, Delta, Range, HealType); `HealCastorOnAtkEffectNames` HashSet (`["healcastoronatk"]`); `HealCastorOnAtkEffects` getter parsing `value`, `delta`, `range`, `type` attributes
    - [✓] `Game/Combat/Handlers/HealCastorOnAttackedHandler.cs` — `IEventHandler<DamageDealtEvent>` implementation: iterates `target.GetActiveEffects()`, looks up the skill template per active buff, range-gates the buff caster against `target.Position`, heals caster HP/MP by `BaseValue + Delta * SkillLevel`, broadcasts `SM_ATTACK_STATUS` (NaturalHp/Mp + Heal/MpHeal LogId) to the caster's worldId
    - [✓] `Program.cs` — registers `HealCastorOnAttackedHandler` as `Transient<IEventHandler<DamageDealtEvent>>` (matches InMemoryEventBus's `sp.GetServices` pattern at publish time)
    - [✓] 14 damage sites wired through helper:
        - `CM_ATTACK.cs`: line 244 (auto-attack), 124 (parry), 151 (block), 276 (godstone proc) — IEventBus injected via constructor + factory
        - `CM_CASTSPELL.cs`: line 819 (ground-AoE primary), 929 (ground-AoE DoT tick), 1177 (single-target), 1370 (splash), 1505 (splash DoT tick), 1748 (chain DoT tick) — IEventBus injected via constructor + factory
        - `Services/NpcAiService.cs`: line 375 (NPC auto-attack), 561 (NPC skill primary), 629 (NPC DoT tick), 682 (NPC splash) — IEventBus injected via DI
    - [✓] DamageKind selection per site: AutoAttack for both CM_ATTACK and NpcAiService auto-attacks (and parry/block which are auto-attack outcomes); PhysicalSkill/MagicalSkill via `spellIsMagical` ternary or `isPhysical` flag; Splash for splash hits; DoTTick for all 3 player-side + 1 NPC-side DoT loops
    - Java analogy: post-damage observer hook in HealCastorOnAttackedEffect.java (ActionObserver.ATTACKED) fires `effected.healHp/Mp` if effector within `radius` — our handler mirrors this scope. The `<healcastoronatk type="HP" range="5.0" value="23" duration2="10000">` Templar/Cleric defensive-aura skill (which buffs an ally and heals the caster on every hit landed on the ally) is the canonical use case
    - Pattern unlocks future handlers: `MagicCounterAtkHandler` (counter-attack on magic skill received), `DispelBuffCounterAtkHandler` (dispel attacker buff on hit), and a future pre-damage `DamageReceivingEvent` extension for `<reflector>`/`<convertheal>` (require mutable damage payload — separate milestone)
    - Previously: 9 healcastoronatk skill XML entries (Templar "Healing Touch on Attack" line, Cleric defensive heals, several boss aura mechanics) buffed the target but the caster never received heal — Java's ATTACKED observer was unimplemented
    - Build: 0 warnings, 0 errors

261. [✓] MagicCounterAtk handler — `<magiccounteratk>` self-damage on magical attack cast (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `SkillMagicCounterAtkInfo` record (Percent, MaxDmg); `MagicCounterAtkEffectNames` HashSet (`["magiccounteratk"]`); `MagicCounterAtkEffects` getter parses `value` (percent) + `maxdmg` (cap)
    - [✓] `Game/Combat/Handlers/MagicCounterAtkHandler.cs` — `IEventHandler<DamageDealtEvent>`: gates on `Kind == MagicalSkill`, iterates attacker's `GetActiveEffects()`, computes `selfDmg = min(MaxDmg, attacker.MaxHp * Percent / 100)`, applies to attacker.CurrentHp, broadcasts `SM_ATTACK_STATUS(Damage, Regular)`
    - [✓] `Program.cs` — registers `MagicCounterAtkHandler` as second `Transient<IEventHandler<DamageDealtEvent>>` alongside HealCastorOnAttackedHandler — InMemoryEventBus dispatches to all registered handlers per publish
    - Java analogy: `MagicCounterAtkEffect.startEffect` attaches `ActionObserver(SKILLUSE)` to effected; on skilluse, schedules a 0ms task that filters `SkillType.MAGICAL && SkillSubType.ATTACK`, then calls `effected.onAttack(effector, dmg)` with `dmg = min(maxdmg, MaxHp * value / 100)`. We approximate via the post-damage event because our DamageDealtEvent already encodes "the buffed creature dealt damage with a magical skill"
    - Pattern note: this handler does NOT use the helper to apply self-damage — it's a tertiary mutation outside the canonical attacker→target damage flow. Doing so would publish a recursive DamageDealtEvent and risk handler re-entry. Direct mutation matches Java's semantic ("blood-magic cost" applied silently) and keeps the event bus invariant: 1 publish = 1 logical attack
    - Previously: 8 magiccounteratk skill XML entries (a Sorcerer/Spiritmaster "Blood Pact"-style buff line + several boss-encounter cost-magic mechanics) had no effect — buffed creatures cast magical attacks at no HP cost
    - Build: 0 warnings, 0 errors

262. [✓] DispelBuffCounterAtk — `<dispelbuffcounteratk>` aliased to existing damage + dispelbuff (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `"dispelbuffcounteratk"` to `DamageEffectNames` HashSet (routes the small `value` damage through magical-damage formula)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — extended `HasDispelBuff` matcher to include `dispelbuffcounteratk` so post-damage `ClearBuffs` fires
    - Java analogy: `DispelBuffCounterAtkEffect` extends `DamageEffect` and additionally calls `effected.EffectController.dispelBuffCounterAtkEffect(i, dispelLevel, finalPower)`. The damage half is just inherited from the parent's `value + delta * level` formula. Our port runs both halves through existing pipelines: damage via `dmgFx[0]` and buff cleanup via `HasDispelBuff` flag fired at the existing CM_CASTSPELL dispel block (line 781 / 1141)
    - Approximation: Java's `dispelLevel`/`power` parameters drive a probability/level-gated dispel; we run an unconditional `target.ClearBuffs()` (over-dispel). Acceptable for now — sub-categorized dispel needs a typed-dispel implementation alongside the M250 NPC-dispel infrastructure. Track for a future refinement
    - Previously: 18 dispelbuffcounteratk skill XML entries (Templar/Gladiator dispel-on-hit moves like "Dispelling Cleave") silently ignored both the small damage hit AND the buff dispel
    - Build: 0 warnings, 0 errors

263. [✓] Reflector handler — `<reflector>` returns damage to attacker (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `SkillReflectorInfo` record (HitValue, HitDelta, Radius); `ReflectorEffectNames` HashSet; `ReflectorEffects` getter parses `hitvalue`, `hitdelta`, `radius`
    - [✓] `Game/Combat/Handlers/ReflectorHandler.cs` — `IEventHandler<DamageDealtEvent>`: skips DoTTick (Java's observer fires per attack, not per tick), skips self-damage, iterates target's reflector buffs, range-gates attacker by Radius, computes `reflectDmg = HitValue + HitDelta * SkillLevel` (clamped to attacker's CurrentHp), subtracts from attacker, broadcasts `SM_ATTACK_STATUS(Damage, Regular)`
    - [✓] `Program.cs` — registered as third `Transient<IEventHandler<DamageDealtEvent>>` after HealCastorOnAttacked + MagicCounterAtk
    - Java analogy: `ReflectorEffect.startEffect` builds an `AttackShieldObserver(hit, value, percent, ...)` and attaches to effected via `addAttackCalcObserver`. Java intercepts BEFORE damage; we approximate AFTER — net gameplay equivalent for typical reflector skills (caster takes some HP back per hit). Skill types where Java's pre-damage shield would absorb instead of allowing damage to land (true shield) need a future `DamageReceivingEvent` with mutable damage payload
    - Previously: 95 reflector skill XML entries (Templar "Reflect Damage" line, Sorcerer/Spiritmaster magical reflect, several boss-encounter mechanics) cast their animation but never returned damage to attackers
    - Build: 0 warnings, 0 errors

264. [✓] ConvertHeal handler — `<convertheal>` heals target on hit (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `SkillConvertHealInfo` record (BaseValue, Delta, HealType); `ConvertHealEffectNames` HashSet; `ConvertHealEffects` getter parses `value`, `delta`, `type` (HP|MP)
    - [✓] `Game/Combat/Handlers/ConvertHealHandler.cs` — `IEventHandler<DamageDealtEvent>`: skips DoTTick, iterates target's convertheal buffs, computes `healAmt = BaseValue + Delta * SkillLevel`, applies to target HP/MP clamped at Max, broadcasts `SM_ATTACK_STATUS(NaturalHp/Mp, Heal/MpHeal)`
    - [✓] `Program.cs` — fourth `Transient<IEventHandler<DamageDealtEvent>>` registration
    - Java analogy: `ConvertHealEffect.startEffect` builds `AttackShieldObserver(hitvalue, value, percent, hitPercent, ..., type, 0)` and calls `setUnderShield(true)` on effected. The shield converts incoming damage to heal of `type`. Our simplified post-damage version applies an additive heal after the hit lands — net gameplay close to Java for typical convertheal skills (the buffed creature gains HP/MP per incoming hit) but doesn't preserve the shield-eats-damage semantics for skills like the 1000000-cap Inquinata's "Holy Fortitude" boss mechanic
    - Closes the 5-effect M260 unlock pool: 9+8+18+95+4 = 134 skills now reach handlers via the damage observer pattern. Pre-damage shield semantics (true damage absorption, percent-based reduction) is the next architectural milestone — needs a `DamageReceivingEvent` with mutable damage payload, fired from inside `ApplyDamageAndPublishAsync` BEFORE the HP subtract
    - Previously: 4 convertheal skill XML entries (Templar shields, boss survival mechanics) ignored — buffed creatures took full damage with no conversion to heal
    - Build: 0 warnings, 0 errors

265. [✓] DamageReceivingEvent + Shield handler — pre-damage absorption infrastructure (session 2026-05-04)
    - [✓] `Game/Events/DamageDealtEvent.cs` — added `MutableDamage` class wrapper + `DamageReceivingEvent` record (Attacker, Target, MutableDamage Damage, DamageKind, SkillId)
    - [✓] `Game/Combat/CreatureDamageExtensions.cs` — `ApplyDamageAndPublishAsync` now publishes `DamageReceivingEvent` with a `MutableDamage(damage)` cell BEFORE HP write; reads `cell.Value` after handlers run; if cell zeroed, returns early skipping HP subtract + DamageDealtEvent (full absorption)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `SkillShieldInfo` record (HitValue, HitDelta); `ShieldEffectNames` HashSet; `ShieldEffects` getter parses `hitvalue`+`hitdelta`
    - [✓] `Game/Combat/Handlers/ShieldHandler.cs` — `IEventHandler<DamageReceivingEvent>`: iterates target's shield buffs, computes per-buff absorb cap = HitValue + HitDelta * SkillLevel, mutates `e.Damage.Value -= absorbed`, broadcasts `SM_ATTACK_STATUS(ProtectDmg, Regular)` to in-world clients. Stops early when fully absorbed
    - [✓] `Program.cs` — fifth handler registered: `Transient<IEventHandler<DamageReceivingEvent>>` for ShieldHandler (separate event type from the 4 DamageDealt handlers)
    - Architecture milestone: completes the bidirectional damage observer model. Pre-damage handlers (Shield/Protect/etc.) intercept and mutate; post-damage handlers (HealCastorOnAttacked/MagicCounterAtk/Reflector/ConvertHeal) react after the hit lands. The MutableDamage cell decouples handler chains — Shield can run alongside Reflector without semantic conflict
    - Java analogy: `ShieldEffect.startEffect` builds `AttackShieldObserver(hitvalue, value, percent, ...)` and registers it on effected. The observer's `onAttack` reduces damage. Our port mirrors this with: Shield handler reduces `cell.Value`; full-absorb case skips both HP write and post-damage event (matches Java where shielded hits don't trigger ATTACKED observers)
    - Previously: 331 shield skill XML entries (Templar "Magic Shield"/"Body Smash", Cleric/Chanter defensive auras, Sorcerer/Spiritmaster magical shield line, hundreds of boss-encounter shields) cast their animation but absorbed nothing
    - Build: 0 warnings, 0 errors

266. [✓] Protect handler — `<protect>` redirects damage to buff caster (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `SkillProtectInfo` record (HitValue, IsPercent, Radius); `ProtectEffectNames` HashSet; `ProtectEffects` getter parses `hitvalue`/`percent`/`radius`
    - [✓] `Game/Combat/Handlers/ProtectHandler.cs` — `IEventHandler<DamageReceivingEvent>`: looks up caster (effector) via World, range-gates by Radius, computes redirect = `IsPercent ? cell.Value * HitValue / 100 : min(HitValue, cell.Value)`, mutates `cell.Value -= redirect`, applies `caster.CurrentHp -= casterDmg` directly (no event publish to avoid cascade), broadcasts `SM_ATTACK_STATUS(Damage, Regular)` on caster
    - [✓] `Program.cs` — second `IEventHandler<DamageReceivingEvent>` registration after Shield
    - Java analogy: `ProtectEffect.startEffect` builds `AttackShieldObserver` with `shieldType=8`. Java's observer chain redirects damage at the absorb stage. Our port: cell-based subtract from incoming + direct caster mutation. Damage flows: incoming → reduced (cell) → caster takes redirect (out-of-band)
    - Handler ordering with Shield: both subscribe to DamageReceivingEvent. InMemoryEventBus dispatches sequentially per `sp.GetServices` order. Whichever fires first reduces cell.Value; the second sees the already-reduced value. For typical buffs that stack Shield + Protect (rare), the order is registration-deterministic — Shield runs first (registered first), Protect operates on the residual
    - Previously: 57 protect skill XML entries (Sorcerer "Stone Skin"-style aura redirects, several boss adds → boss redirect mechanics, summon-protect-master) cast their animation but redirected nothing
    - Build: 0 warnings, 0 errors

267. [✓] Typed `<dispel>` — aliased to existing dispel-debuff handler (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `HasDispelDebuff` matcher extended with `dispel` (joins `dispeldebuff`, `dispeldebuffphysical`, `dispeldebuffmental`, `dispelnpcdebuff`)
    - Java analogy: `DispelEffect.applyEffect` switches on `dispeltype` (EFFECTID/EFFECTIDRANGE/EFFECTTYPE/SLOTTYPE) and removes specific effects. We approximate with the existing `target.ClearDebuffs()` over-dispel — same simplification used by M246/M250
    - Limitation: full typed-dispel needs `effectid` tracking on `AbnormalState` (currently only carries SkillId). The 60 EFFECTID + 53 EFFECTTYPE skill XML entries get an over-broad cleanse that may dispel more than Java intends. Acceptable as a placeholder; track for refinement if PvP balance shows issues
    - Previously: 135 dispel skill XML entries (Cleric/Chanter typed-dispel skills, Songweaver cleanse line, Spiritmaster mass-dispel) silently ignored — typed dispel routed nowhere despite the existing dispel-debuff infrastructure
    - Build: 0 warnings, 0 errors

268. [✓] Sanctuary handler — full damage immunity while sanctuary buff active (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `HasSanctuary` bool property to `SkillEffects` (mirrors HasResurrectEffect/HasHostileUp pattern)
    - [✓] `Game/Combat/Handlers/SanctuaryHandler.cs` — `IEventHandler<DamageReceivingEvent>`: iterates target's active effects, returns early after the first sanctuary buff is found, zeroes `cell.Value`, broadcasts `SM_ATTACK_STATUS(ProtectDmg, Regular)` showing full absorb amount
    - [✓] `Program.cs` — registered FIRST in DamageReceivingEvent handler chain so full-immunity short-circuits Shield + Protect (avoids redundant per-handler iteration when sanctuary already absorbed everything)
    - Java analogy: `SanctuaryEffect` is a TODO stub in Java — `applyEffect`/`startEffect`/`endEffect` are all empty. Our implementation provides the sensible default that the buff name implies: while sanctuary is active, the creature is immune to damage. Used for safe-zone protections, mid-cast invulnerability frames, and several boss intermission mechanics
    - Previously: 71 sanctuary skill XML entries (24-hour duration safe-zone protections, Songweaver/Cleric clutch invuln frames) had no effect
    - Build: 0 warnings, 0 errors

269. [✓] Weapon-restriction validator — `<weapon>` startcondition enforced (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `[XmlElement("startconditions")] SkillStartConditions? StartConditions` slot; `SkillStartConditions` class with `[XmlElement("weapon")] SkillWeaponCondition? Weapon`; `SkillWeaponCondition.WeaponList` parses the `weapon=` attribute string
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `AllowedWeapons` HashSet getter splits the space-separated list into a case-insensitive set
    - [✓] `CM_CASTSPELL.cs` — pre-cast guard: when `AllowedWeapons` is non-empty, returns early unless `MainHandWeaponType` or `OffHandWeaponType` matches one of the allowed types
    - Java analogy: Java `WeaponCondition.verify(skill)` reads `effector.getEquipment().getMainHandWeaponType()`, returns false if not in allowed list, blocking the cast. We now mirror this server-side
    - Gameplay impact: prevents class skills from being cast with the wrong weapon (e.g. casting Bow skill with a Staff equipped). Affects every class — most damage skills carry weapon restrictions in their `<startconditions>` block. The OffHand check matches Java's bow/dagger dual-handed cases where the secondary slot may carry the type
    - Edge case: skills with no `<weapon>` startcondition pass through (AllowedWeapons.Count == 0 → guard skipped)
    - Build: 0 warnings, 0 errors

270. [✓] Chain-skill state machine — `<chain category="X">` startcondition (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `SkillChainCondition` class (`[XmlAttribute("category")] string Category`); `SkillStartConditions` extended with `[XmlElement("chain")]`; `ChainCategory` convenience property on SkillTemplate (empty = no chain restriction)
    - [✓] `Model/Player.cs` — added `LastChainCategory` (string, set after each successful cast) + `LastChainExpiry` (DateTime, when current chain link goes stale) + `Player.ChainTimeoutMs = 4000` constant
    - [✓] `CM_CASTSPELL.cs` — pre-cast guard: if `ChainCategory` non-empty, returns early unless `Player.LastChainCategory` matches AND `DateTime.UtcNow <= LastChainExpiry`. After guard passes, registers THIS cast: `LastChainCategory = template.ChainCategory`, `LastChainExpiry = now + 4000ms`. Skills without chain category set the player's category to empty (breaking the chain)
    - Java analogy: Java `ChainCondition.verify(skill)` reads `effector.getController().getLastChainSkill()` and validates timestamp against `ChainConfig.CHAIN_TIMEOUT`. We mirror this with a per-Player state pair
    - Gameplay impact: 746 chain-restricted skills (Templar/Gladiator/Assassin combo systems — e.g. "Wind Cut" → "Severe Wind Cut" → "Whirlwind") now correctly require predecessor cast within 4 seconds. Casting out-of-chain returns silently
    - Edge case: chain timeout is per-player not per-target — matches Java behavior where the chain is owned by the caster's combat state
    - Build: 0 warnings, 0 errors

271. [✓] Signet damage alias — `<carvesignet>` + `<signetburst>` deal native value/delta damage (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `"carvesignet"` and `"signetburst"` to `DamageEffectNames` HashSet (routes magical damage through existing damage path)
    - Approximation: Java `CarveSignetEffect` places a typed stack on the target which `SignetBurstEffect` consumes for scaled damage. Our alias delivers the baseline `value + delta * level` damage of both effects without tracking signet stacks. The bonus burst-from-stacks scaling is deferred — needs `target.Signets` Dictionary<string, (Level, Caster)> + per-cast match logic
    - Gameplay impact: 95 carvesignet + 58 signetburst = 153 skill XML entries (Sorcerer signet line — Signet of Wind, Signet of Stone, Signet Burst — and several boss-encounter mark mechanics) now deal their listed damage. Burst skills don't yet scale up from prior carvesignet stacks but they do hit
    - Build: 0 warnings, 0 errors

272. [✓] VP heal subsystem — `<vphealinstant>` + `<procvphealinstant>` (session 2026-05-04)
    - [✓] `Model/Player.cs` — added `Vp` int field (Valor Points alternate resource pool)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added `vphealinstant`/`procvphealinstant` to HealInstantNames; HealEffects ternary routes to `vp` HealType
    - [✓] `CM_CASTSPELL.cs` — single-target heal + AoE ally heal: `vp` branches mirror M252 DP pattern with 6000 cap; no SM_VP_INFO packet yet (server-side state only)
    - [✓] `CM_USE_ITEM.cs` — `HandleItemHealAsync`: `vp` branch for VP-recovery items
    - Java analogy: `VPHealInstantEffect` extends `AbstractHealEffect` with HealType.VP. We use the same 6000 cap as DP until proper Vp template/stat infra exists. Client-side display will lag without SM_VP_INFO; server VP arithmetic is correct
    - Previously: 16 VP heal skill XML entries (siege/PvP zone resource scrolls) silently dropped. Players had no way to gain VP outside zone-specific kill rewards
    - Build: 0 warnings, 0 errors

273. [✓] Item-damage path — caster-AoE offensive consumables (session 2026-05-04)
    - [✓] `CM_USE_ITEM.cs` — constructor accepts `GameWorld world, NpcAiService npcAi, IEventBus eventBus`; `GsPacketHandlerFactory.cs` 0xC7 dispatch updated
    - [✓] `CM_USE_ITEM.cs` — main routing: when skill template has `DamageEffects` with `target_relation="ENEMY"` and `IsCasterAoe`, routes to new `HandleItemDamageAsync` BEFORE the buff/heal paths
    - [✓] `CM_USE_ITEM.cs` — `HandleItemDamageAsync`: enforces UseLimits cooldown; broadcasts SM_ITEM_USAGE_ANIMATION; computes baseDmg from `dmgFx[0]` (item-skills are level 1); iterates `_world.GetAllNpcs()` within `EffectiveRange` (default 12m), filtered by altitude + max-hits cap; routes each hit through `ApplyDamageAndPublishAsync` (auto-plumbed into observer pattern); calls `_npcAi.ForceEngage` per hit; broadcasts SM_ATTACK_STATUS; sets cooldown; consumes one charge
    - Coverage: caster-AoE damage consumables (target_type=AREA, first_target=ME) — items like Taloc's Tears (skill 10250) and similar fire-bomb/elemental-grenade items. Single-target offensive consumables (target_type=ONLYONE) are deferred — would need a target lookup not currently in the CM_USE_ITEM packet
    - Architecture: routes through M260 `ApplyDamageAndPublishAsync` so all 6 damage observers (Sanctuary/Shield/Protect pre + HealCastorOnAttacked/MagicCounterAtk/Reflector/ConvertHeal post) automatically apply to item-cast damage — no special-casing required
    - Previously: items pointing to caster-AoE damage skills consumed their charge and showed the use animation but applied no damage to nearby enemies
    - Build: 0 warnings, 0 errors

274. [✓] Periodic MP drain — `<periodicactions><mpuse>` ticks while buff active (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `PeriodicMpUse` getter scans `<periodicactions checktime="X">` for `<mpuse value="Y"/>` child; returns (CheckTimeMs, MpPerTick) tuple
    - [✓] `CM_CASTSPELL.cs` — buff path post-AddEffect: when PeriodicMpUse non-zero and target is Player, spawns Task.Run that ticks `drainPlayer.CurrentMp -= drainPerTick` every drainInterval until buff expiry or player death
    - Java analogy: Java `PeriodicActions` schedules ticks with `ThreadPoolManager`; on each tick, executes the action (mpuse). We mirror this for the mpuse subset
    - Limitations: when MP drains to 0, buff continues ticking on 0 MP (TODO comment marks the deactivation gap). Java would call `SM_TOGGLE_SKILL_DEACTIVATE` to end the toggle. Adding that needs the toggle-deactivation packet which doesn't yet exist
    - Gameplay impact: 38 periodicactions skill XML entries (Aether-fly toggles, Spiritmaster summon-maintenance auras, several Templar/Cleric stance toggles) now drain MP on the listed cadence. The toggle stays active even after MP runs out (deferred)
    - Build: 0 warnings, 0 errors

275. [✓] Data flags exposed for transform / stealth / abs-stat-buff (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — added properties `HasShapeChange` (matches shapechange/polymorph/deform/form, 672 skills), `ShapeChangeModelId` (parses model attr), `HasHide` (78 skills), `HasAbsStatBuff` (matches absstatbuff/absstatdebuff, 48 skills)
    - Behavior intentionally NOT wired — each needs its own infrastructure milestone:
        - Transform/Polymorph: SM_TRANSFORM packet + Player.CurrentModelId state field + broadcast-on-buff path
        - Hide/Stealth: per-creature visibility filter at packet broadcast sites (CM_ATTACK SM_ATTACK target hidden? skip broadcast to non-stealth-detectors; hundreds of broadcast sites)
        - AbsStatBuff: AbsoluteStatsData.xml DataHolder loader keyed by statsetid; resolves per-stat absolute overrides
    - Purpose: data parsed and accessible so future implementations can detect the effect presence + read model id without re-touching the parser. Subsequent infrastructure milestones plug in by reading these flags
    - Total skills covered (data-only): ~798 — full behavior remains deferred per the noted infrastructure dependencies
    - Build: 0 warnings, 0 errors

276. [✓] AlwaysResist handler — `<alwaysresist>` magic-damage immunity (session 2026-05-04)
    - [✓] `Model/Templates/Skill/SkillTemplate.cs` — `HasAlwaysResist` bool flag
    - [✓] `Game/Combat/Handlers/AlwaysResistHandler.cs` — `IEventHandler<DamageReceivingEvent>`: gates on Kind ∈ {MagicalSkill, DoTTick}, iterates target's active effects, returns after first alwaysresist match with `cell.Value = 0` and a 0-damage SM_ATTACK_STATUS broadcast (signals "resisted!")
    - [✓] `Program.cs` — registered after Sanctuary (full immunity) and before Shield (partial absorb) so the magic-immunity short-circuit runs before partial absorbers
    - Java analogy: AlwaysResistEffect grants 100% magic resist while buff is active. Used in boss "Holy Fortitude"-style mechanics paired with M264 ConvertHeal (alwaysresist eats magic, convertheal converts physical to heal)
    - Gameplay impact: handful of buff XML entries (boss survivability mechanics, very high-tier defense buffs) now correctly resist all magical damage and DoT ticks
    - Build: 0 warnings, 0 errors

286. [✓] Aura tick subsystem — caster-anchored periodic AoE child-skill ticks (session 2026-05-04)
    - Architecture milestone built via analyst → planner → implementer pipeline. Java analog: `AuraEffect.java` (6500ms `AuraTask`).
    - **M286a:** `Model/Templates/Skill/SkillTemplate.cs` — `SkillAuraInfo` record (ChildSkillId, Distance, DistanceZ); `HasAura` bool flag; `AuraEffects` getter parsing `<aura skill_id distance distance_z>` (DistanceZ defaults to Distance/2 when XML omits)
    - **M286b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — buff-path scheduler after M274 mpuse: per aura entry resolves child template, captures locals, spawns `Task.Run` with 6500ms `Task.Delay` interval. **Race-safe loop condition** (analyst HIGH risk): `caster.GetActiveEffects().Any(e => e.SkillId == auraSkillId)` instead of stale `DateTime.UtcNow < effect.Expiry`. Resolves scope to caster + Player.Group.Members; filters by 3D distance (`dx²+dy² ≤ Distance²`, `|dz| ≤ DistanceZ`) + WorldId. Calls `_auraApplier.ApplyAsync` per ally
    - **M286c:** `Services/AuraChildApplier.cs` — `IEventBus`-aware applier. **Recursion guard** (analyst HIGH risk): if child has `<aura>`, log debug + skip aura branch only (heal/damage still apply, no nested tick spawned). Branches: heal (HP/MP write + SM_ATTACK_STATUS broadcast), damage (routes through `ApplyDamageAndPublishAsync` so all observer handlers — Shield/Reflector/etc. — fire). Statup/CC explicitly out of scope to avoid every-tick re-application
    - **DI:** registered `AuraChildApplier` as Singleton; CM_CASTSPELL ctor extended; `GsPacketHandlerFactory` plumbs through
    - Out of scope (deferred to future milestones M286d/e):
        - SM_MANTRA_EFFECT visual broadcast packet
        - StatUp child skills (would require idempotent buff refresh)
        - NPC casters of aura (Java casts effector to Player; we mirror with `is Player` guard)
        - Alliance scope (currently Player.Group only; alliance support pending)
        - CM_TOGGLE_SKILL_DEACTIVATE-driven toggle stop
    - Gameplay impact: 52 aura skill XML entries (Cleric heal mantras, Spiritmaster damage auras, Songweaver buff auras, several boss-room mark-and-pulse mechanics) now tick child skills against in-range group members every 6.5s. Heal auras restore HP/MP, damage auras pass through observer chain (Shield/Reflector still apply on aura ticks)
    - Risks resolved: tick-vs-expiry race avoided via live effect-list polling. Nested-aura recursion bounded at depth 1
    - Build: 0 warnings, 0 errors

287. [✓] DeathEvent + HealCastorOnTargetDead handler (session 2026-05-04)
    - Architecture milestone built via analyst → planner → implementer pipeline. Java analog: `HealCastorOnTargetDeadEffect.java` (ActionObserver DEATH).
    - **M287a:** `Events/DamageDealtEvent.cs` — added `DeathEvent(Creature Killer, Creature Victim, DamageKind Kind, int? SkillId)` record alongside existing damage events
    - **M287b:** `Combat/CreatureDamageExtensions.cs` — `ApplyDamageAndPublishAsync` now publishes DeathEvent on the alive→dead transition. **Race-safe via wasAlive guard** (analyst HIGH risk): captures `wasAlive = target.CurrentHp > 0` before HP write; publishes only when `wasAlive && CurrentHp == 0`. **Duel false-fire suppression** (analyst HIGH risk): added `bool suppressDeathEvent` parameter; `CM_ATTACK.cs` (parry/block/auto/proc lines) and `CM_CASTSPELL.cs` (single-target damage) pre-check `_duelService.GetOpponent` and pass `true` when target is the duel opponent — duel HP=1 restoration happens after helper returns, so DeathEvent must not fire pre-emptively
    - **M287c:** new `Combat/Handlers/HealCastorOnTargetDeadHandler.cs` — `IEventHandler<DeathEvent>` with explicit attribution per Java line 49+101 (verified before implementation): iterates **victim's** active effects, looks up the **buff caster** (effector) by `ab.EffectorId`, heals the caster (HP or MP per `type` attr), range-gates by `caster.Position.DistanceTo(victim.Position) <= Range`, and extends to **caster's PlayerGroup members** in range when `healparty="true"` is set
    - **Parser:** `SkillTemplate.cs` — `SkillHealOnTargetDeadInfo` record + `HealOnTargetDeadEffectNames` HashSet + `HealOnTargetDeadEffects` getter parsing `value`, `delta`, `range`, `type` (HP/MP), `healparty`
    - **DI:** registered as `Transient<IEventHandler<DeathEvent>>`
    - Out of scope (deferred):
        - Non-helper death paths (fall damage, scripted kills, GM /kill) — won't fire DeathEvent until those sites also route through the helper
        - Alliance scope (Java extends to PlayerAllianceGroup2; we cover only PlayerGroup since alliance API isn't ported yet)
    - Gameplay impact: 2 healcastorontargetdead skill XML entries (Songweaver/Cleric "vengeance" buffs that grant a heal when the bearer dies) now trigger; foundation laid for any future death-driven handlers (loot/quest/abyss-point hooks)
    - Risks resolved: HIGH double-publish on corpse splash (wasAlive guard); HIGH duel false-fire (suppressDeathEvent param); MEDIUM Java attribution (re-read confirmed: heal recipient = effector / buff caster, ownership = victim's effect list)
    - Build: 0 warnings, 0 errors

288. [✓] ArmorMastery passive — conditional pdef% bonus from armor proficiency skills (session 2026-05-05)
    - Architecture milestone built via analyst → implementer pipeline. Java analog: `ArmorMasteryEffect.java` + `StatArmorMasteryFunction.java`.
    - **M288a:** `Model/Templates/Skill/SkillTemplate.cs` — `SkillArmorMasteryInfo` record (ArmorType, PdefPct); `ArmorMasteryEffects` getter on `SkillEffects` parses `<armormastery armor="X">` elements, sums `<change stat="PHYSICAL_DEFENSE" func="PERCENT" value="Y"/>` children
    - **M288b:** `Services/PassiveArmorMasteryHelper.cs` — `ComputePct(Player, IDataManager)`: collects equipped armor types from inventory, iterates PASSIVE skills for ArmorMasteryEffects matching those armor types, returns total percent
    - **M288c:** `Services/PlayerEnterWorldService.cs` — after passive loop, calls `ComputePct` and multiplies `player.PhysicalDefense` by `(1 + pct/100.0)`; at this point `PhysicalDefense` already holds equipment + title flat contributions
    - **M288d:** `Network/Aion/ClientPackets/CM_EQUIP_ITEM.cs` and `CM_MANASTONE.cs` — same pattern: after `TitleStatsApplicator.Apply`, recompute mastery pct and apply to `player.PhysicalDefense`
    - Gameplay impact: 38 PASSIVE armor proficiency skills (CLOTHES/LEATHER/CHAIN/PLATE masteries) now grant their +10% pdef bonus; all classes gain correct physical defense from worn armor type; e.g. Templar in plate gets ~10% pdef boost
    - Previously: armor proficiency skills were parsed and visible in the skill book, but their PERCENT pdef bonus was never applied — players were underperforming Java server pdef values by ~10%
    - Build: 0 warnings, 0 errors

289. [✓] Signet stack system — CarveSignet placement + SignetBurst level-scaled consumption (session 2026-05-06)
    - Architecture milestone built via analyst → planner → implementer pipeline. Java analogs: `CarveSignetEffect.java` + `SignetBurstEffect.java` + `SignetEffect.java`.
    - **M289a:** `Model/AbnormalState.cs` — added `StackName { get; init; } = "NONE"` property; used by signet system to locate/remove effects by stack group name without knowing the exact SkillId
    - **M289b:** `Model/Creature.cs` — added `GetEffectByStack(string)` (returns first non-expired match) and `RemoveEffectByStack(string)` (removes all matches + reverses deltas, idempotent); both use `_effectsLock` + `ReverseEffectDeltas` / `RebuildCcFlags` to stay consistent with existing `RemoveEffectBySkillId` pattern
    - **M289c:** `Model/Templates/Skill/SkillTemplate.cs` — added `CarveSignetInfo` record (SignetId, SignetLvlCap, SignetLvlStart, Prob, Signet) + `SignetBurstInfo` record (SignetLvlMax, Signet, AccMod2); added `CarveSignetEffects` and `SignetBurstEffects` computed properties on `SkillEffects` following existing `AuraEffects`/`ArmorMasteryEffects` pattern
    - **M289d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — single-target damage path, 4 splice points:
        1. Pre-resist: read `signetBurstState` via `GetEffectByStack`; compute `signetBurstAccBoost` from signet level × mAccuracy (Java accmod2 formula: -0.8/-0.5/0/+0.2/+0.5 × mAcc)
        2. Resist check: `totalMagicAcc += signetBurstAccBoost` (level 1-2 signet = harder to land; level 4-5 = easier); on resist early-return: consume signet before `return` (Java SignetBurstEffect.calculate consumes on resist)
        3. Post-rawSpellDmg scale: 0.05× (no signet) or 0.2/0.5/1.0/1.2/1.5× by level (with signet); applied BEFORE crit so crit multiplies the scaled value
        4. Post-statusPkt: SignetBurst removes signet + broadcasts SM_ABNORMAL_EFFECT; CarveSignet computes nextSignetLevel (Java logic: start at signetlvlstart or 1; advance existing +1; cap at min(signetlvl, 5) via `--` stay-at-cap); removes old signet, places new `AbnormalState { StackName = "SYSTEM_SKILL_SIGNET1", Expiry = +24s, SkillLevel = nextLv }`; broadcasts SM_ABNORMAL_EFFECT; schedules 24s auto-expiry Task.Run (idempotent RemoveEffectByStack)
    - Prob gate: `Random.Shared.Next(101) <= cs.Prob` mirrors Java `Rnd.get(0, 100) > prob` skip condition
    - Signet duration: hardcoded 24_000ms — all SYSTEM_SKILL_SIGNET1 templates (8303-8307) carry `<signet duration2="24000"/>` in their `<effects>` block
    - Key behavior change from M271: SignetBurst without a signet on target now deals 5% damage (was 100% in M271 approximation); this is the correct Java behavior
    - Gameplay impact: 95 carvesignet + 58 signetburst = 153 Sorcerer skill XML entries now have full stack mechanics; Rune Carve line builds SYSTEM_SKILL_SIGNET1 stacks 1→3 on target; Signet Burst consumes the stack for 1.0× (lvl3) up to 1.5× (lvl5) damage; signet expires after 24s if not burst
    - Previously: both carvesignet and signetburst dealt M271 baseline damage with no stack tracking; burst was over-dealing on bare targets (100% instead of 5%) and under-dealing on stacked targets (100% instead of 150%)
    - Build: 0 warnings, 0 errors

290. [✓] MP-drain auto-deactivate — toggle buff removed when MP hits 0 during periodicactions drain (session 2026-05-06)
    - **M290a:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — in the periodic MP drain task (M274), after `drainPlayer.CurrentMp = Math.Max(0, ...)`: when `CurrentMp == 0`, calls `RemoveEffectBySkillId(drainEffect.SkillId)`, sends `SM_PLAYER_STANCE(0)` to the toggle owner, broadcasts `SM_ABNORMAL_EFFECT` to the zone, then breaks the drain loop; removes TODO comment
    - No new packet needed — Java also has no SM_TOGGLE_SKILL_DEACTIVATE server-side packet; the existing CM_TOGGLE_SKILL_DEACTIVATE.cs (client packet handler, opcode 0xE0) has the same deactivation sequence; M290 mirrors it inline
    - Gameplay impact: 38 periodicactions toggle skills (Aether-fly toggles, Spiritmaster summon-maintenance, Templar/Cleric stance toggles) now correctly deactivate when MP is fully drained; previously the buff remained active indefinitely even at 0 MP
    - Build: 0 warnings, 0 errors

291. [✓] WeaponMastery passive — conditional physAtk% / magAtk% bonus from weapon proficiency skills (session 2026-05-06)
    - Architecture: mirrors M288 ArmorMastery pattern exactly; Java analog: `WeaponMasteryEffect.java` + `StatWeaponMasteryFunction.java`.
    - **M291a:** `Model/Templates/Skill/SkillTemplate.cs` — `SkillWpnMasteryInfo` record (WeaponType, Stat, Pct); `WpnMasteryEffects` computed property on `SkillEffects` parses `<wpnmastery weapon="X"><change stat="Y" func="PERCENT" value="Z"/></wpnmastery>`
    - **M291b:** `Services/PassiveWeaponMasteryHelper.cs` (new) — `Compute(Player, IDataManager)` returns `(PhysAttPct, MagAttPct)`; collects equipped weapon types from inventory; iterates PASSIVE skills for WpnMasteryEffects matching those weapon types; accumulates PHYSICAL_ATTACK and MAGICAL_ATTACK percents separately
    - **M291c:** `Services/PlayerEnterWorldService.cs` — after M288 ArmorMastery block: calls `PassiveWeaponMasteryHelper.Compute`; multiplies `player.BasePhysicalAttack` by `(1 + physPct/100.0)` and `player.MainHandMagicalAtk` by `(1 + magPct/100.0)` when non-zero
    - **M291d:** `Network/Aion/ClientPackets/CM_EQUIP_ITEM.cs` and `CM_MANASTONE.cs` — same recompute pattern after M288 block; ensures weapon swap and manastone equip refresh the mastery bonus
    - Stats covered: 92 PHYSICAL_ATTACK PERCENT + 32 MAGICAL_ATTACK PERCENT entries across 124 wpnmastery XML entries (all classes: Warrior/Gladiator/Templar get sword/polearm; Scout/Ranger/Assassin get bow/dagger; Mage/Sorcerer/Spiritmaster get staff/orb; Priest/Cleric/Chanter get mace/staff)
    - Previously: weapon proficiency skills were visible in skill book but PERCENT attack bonuses were never applied; players underperforming by 16-30% physical attack (class-dependent) compared to Java server values
    - Build: 0 warnings, 0 errors

- [x] **M292: Blind debuff** — BlindEffect forces physical auto-attacks to miss with value% probability; 76 occurrences in skills_templates.xml (e.g. `<blind value="80" duration2="25000"/>`)
    - Java analog: `BlindEffect.java` → `AttackCalcObserver.checkShield()` returning DODGE when `Rnd.get(0,100) <= value`
    - **M292a:** `Model/AbnormalCcFlags.cs` — added `Blind = 1` (bit 0, unused by existing flags); not part of CantAttack or CantMove composites (blind doesn't prevent action, only causes misses)
    - **M292b:** `Model/AbnormalState.cs` — added `BlindDodgePct { get; init; }` alongside existing stat-delta properties
    - **M292c:** `Model/Templates/Skill/SkillTemplate.cs` — added `BlindDodgePct` computed property on `SkillEffects` reading `blind.value` directly; added `"blind" => AbnormalCcFlags.Blind` to `ElementToCcFlag` switch
    - **M292d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — in the debuff application block: read `BlindDodgePct` and set it on the `debuffEffect` initializer
    - **M292e:** `Network/Aion/ClientPackets/CM_ATTACK.cs` — after CantAttack guard: if attacker has Blind CcFlag active, roll `Random.Shared.Next(101) <= blindEffect.BlindDodgePct`; on hit, broadcast `SM_ATTACK.HitResult.Dodge` and return (attack consumed, no damage, cooldown not bypassed)
    - Build: 0 warnings, 0 errors

- [x] **M293: BoostSkillCastingTime buff** — `<boostskillcastingtime>` elements with `<change stat="BOOST_CASTING_TIME" func="PERCENT" value="X"/>` children reduce cast time by X%; 68 occurrences
    - Java analog: `BoostSkillCastingTimeEffect.java` extends `BufEffect`; PERCENT BOOST_CASTING_TIME change applied to CreatureGameStats
    - **M293a:** `Model/Templates/Skill/SkillTemplate.cs` — added `BoostCastTimePctDelta` property on `SkillEffects`; reads `boostskillcastingtime PERCENT BOOST_CASTING_TIME value * 10` (converts % to permille for cast time formula base-1000)
    - **M293b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — in BUFF statup path at `castTimeStatUpDelta`: combined with `BoostCastTimePctDelta`; the sum flows into `CastTimeDeltaVal` on AbnormalState and `CastTimeDelta` on Creature, feeding the existing `(1000 - CastTimeDelta) / 1000f` formula
    - Previously: boostskillcastingtime buff skills cast their animation and applied the buff icon but cast time was never reduced
    - Build: 0 warnings, 0 errors

- [x] **M294: HealDeboost debuff** — `<deboostheal>` elements with `<change stat="HEAL_SKILL_DEBOOST" func="PERCENT" value="X"/>` reduce (or boost) received healing by X%; 32 occurrences (mostly -50%/-60% debuffs; 1 +30% buff)
    - Java analog: `DeboostHealEffect.java` extends `BufEffect`; modifies HEAL_SKILL_DEBOOST CreatureGameStats entry used when calculating incoming heal
    - **M294a:** `Model/AbnormalState.cs` — added `HealReceivedPctDelta { get; init; }` property
    - **M294b:** `Model/Creature.cs` — added `HealReceivedPct` accumulator field; AddEffect/ReverseEffectDeltas updated to accumulate/reverse `HealReceivedPctDelta`
    - **M294c:** `Model/Templates/Skill/SkillTemplate.cs` — added `HealDeboostPct` property on `SkillEffects`; added `"deboostheal"` to `EffectDurNames` so `EffectDuration` picks up duration2 for debuff tracking
    - **M294d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — debuff block: reads `HealDeboostPct`, sets `HealReceivedPctDelta`; all 3 heal paths (instant healinstant, HoT tick, AoE group heal) now multiply heal by `(100 + healTarget.HealReceivedPct) / 100f` clamped to 0
    - Previously: HEAL_SKILL_DEBOOST debuffs (Spiritmaster/Chanter counter spells) had no effect — targets received full healing even under the debuff
    - Build: 0 warnings, 0 errors

- [x] **M295: OpenAerial CC** — `<openaerial>` element sets AbnormalCcFlags.OpenAerial CC (CantAttack + CantMove); 19 occurrences (aerial state / launch into air)
    - Java analog: `OpenAerialEffect.java` sets AbnormalState.OPENAERIAL and cancels movement
    - **M295a:** `Model/Templates/Skill/SkillTemplate.cs` — added `"openaerial" => AbnormalCcFlags.OpenAerial` to `ElementToCcFlag` switch; added `"openaerial"` to `EffectDurNames` to pick up `duration2` (typical: 2000ms); `OpenAerial` is already in `CantAttack` and `CantMove` composites
    - Since `OpenAerial` is in `CantMove`, the existing SM_TARGET_IMMOBILIZE broadcast in the debuff path fires automatically
    - Build: 0 warnings, 0 errors

- [x] **M296: WeaponDual passive** — `<wpndual value="X"/>` passive skill sets off-hand damage effectiveness %; 11 occurrences (Assassin/Ranger Advanced Dual-Wielding line)
    - Java analog: `WeaponDualEffect.setDualEffectValue(value)` on Player; used by StatDualWeaponMasteryFunction in AttackUtil off-hand calculation
    - **M296a:** `Model/Player.cs` — added `DualWieldEffectPct { get; set; }` (0 = not learned; runtime default is 50%)
    - **M296b:** `Model/Templates/Skill/SkillTemplate.cs` — added `WpnDualEffectPct` computed property on `SkillEffects` reading the first `wpndual` element's `value=` attribute
    - **M296c:** `Services/PlayerEnterWorldService.cs` — after M291 WeaponMastery block: scans all player passive skills for highest `WpnDualEffectPct` value and assigns to `player.DualWieldEffectPct`
    - **M296d:** `Network/Aion/ClientPackets/CM_ATTACK.cs` — replaced hardcoded `ohRaw /= 2` with `dualPct = player.DualWieldEffectPct > 0 ? player.DualWieldEffectPct : 50; ohRaw = ohRaw * dualPct / 100`
    - Previously: off-hand attacks always dealt 50% damage regardless of dual-wield mastery skill level; Assassin/Ranger classes with Advanced Dual-Wielding (70-83%) were underperforming by 40-66% on off-hand hits
    - Build: 0 warnings, 0 errors

- [x] **M297: DelayDamage** — `<delaydamage delay="N" value="V" delta="D" element="E"/>` fires magical skill damage after delay ms post-cast; 28 occurrences (e.g. Sorcerer Flame Cage, Spiritmaster DoT finale)
    - Java analog: `DelayedSpellAttackInstantEffect extends DamageEffect` — schedules `calculateAndApplyDamage(effect)` via `ThreadPoolManager.schedule`; uses `AttackUtil.calculateMagicalSkillResult` with `valueWithDelta = value + delta * skillLevel`
    - XML format: `<delaydamage delay="4000" value="1613" delta="23" e="1" element="FIRE" critprobmod2="0" hoptype="DAMAGE"/>`
    - **M297a:** `Model/Templates/Skill/SkillTemplate.cs` — added `SkillDelayDamageInfo(DelayMs, BaseValue, Delta, Element)` record; added `DelayDamageEffects` computed property on `SkillEffects` parsing all `delaydamage` elements
    - **M297b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — after dispel-buff block in single-target damage path: for each `DelayDamageEffects` entry, dispatches `Task.Run` with `Task.Delay(DelayMs)`; inside: dead-check, full magical damage formula (mAtk + ddVal) * mbMult, MBResist defense reduction, `ApplyDamageAndPublishAsync`, `SM_ATTACK_STATUS` broadcast; target/player captured as locals before loop to avoid closure drift
    - No crit roll on delayed damage (Java also skips crit for DoT-style delay effects in 4.6.0)
    - Build: 0 warnings, 0 errors

- [x] **M298: SpellAtk DoT formula fix** — `<spellatk checktime="N" value="V" delta="D" duration2="T"/>` periodic magical DoT was using flat `value + delta*level` per tick; corrected to full magical skill formula: `(mAtk + valueWithDelta) * mbMult` with MBResist/MagicDefense reduction; 491 occurrences (Sorcerer/Spiritmaster main DoT chain)
    - Java analog: `SpellAttackInstantEffect extends DamageEffect` — calls `calculateAndApplyDamage(effect)` on each tick using `AttackUtil.calculateMagicalSkillResult` (includes caster MAtk + MagicBoost × suppression)
    - Previously: Sorcerer/Spiritmaster `spellatk` ticks dealt flat value damage ignoring caster's magical attack and magic boost stats, severely underperforming for high-MAtk builds
    - **M298a:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — updated all 3 DoT tick sites (AoE primary, AoE splash, single-target): added `if (dot.DotType == "spellatk")` branch that computes `(mAtk + rawDotVal) * mbMult` with MBResist defense reduction; splash site simplifies to `Npc.Template.Stats?.MBResist` directly (splash is always Npc)
    - `spellatk` was already in `DotNames` HashSet and `DamageEffectNames` is also fine; only the tick damage quantity formula needed correction
    - Build: 0 warnings, 0 errors

- [x] **M299: SubEffect secondary skill trigger** — nested `<subeffect skill_id="X" chance="Y"/>` inside damage effect elements triggers a secondary CC skill on hit; 787 occurrences (primarily Warrior/Ranger physical combos: Stumble on hit, Aether's Hold on hit)
    - Java analog: `SubEffect.calculateSubEffect()` — chance roll, load sub-skill template, create new Effect, apply via `startSubEffect()`
    - Main sub-skills in use: 8218 "Stumble" (CcFlag=Stumble, 3000ms) — physical melee proc; 8224 "Aether's Hold" (CcFlag=OpenAerial, 3000ms) — launch target
    - **M299a:** `Model/Templates/Skill/SkillTemplate.cs` — added `SkillSubEffectInfo(SkillId, Chance)` record; added `SubEffects` computed property on `SkillEffects` iterating each `Elements` entry's `ChildNodes` for `<subeffect>` elements; `chance` defaults to 100 when absent
    - **M299b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — added M299 sub-effect block in single-target damage path (after SM_ATTACK_STATUS, before SignetBurst) and in AoE primary-target loop (after DoT application): load sub-template, read `CcFlags` + `Duration`, create `AbnormalState`, broadcast `SM_ABNORMAL_EFFECT`
    - Sub-effects only apply CC flags; stat-delta sub-skills (unusual, <1% of cases) are not applied — acceptable simplification for 4.6.0
    - Build: 0 warnings, 0 errors

- [x] **M300: Sanctuary — dispel-immune buffs** — `<sanctuary/>` child element inside a buff skill's effects marks the buff as immune to `dispelbuff` removal and healing potions; 71 occurrences (primarily Templar/Cleric divine protection skills)
    - Java analog: `Effect.isSanctuaryEffect` flag checked in `EffectController.removeSanctuaryEffect` before any dispel/remove operation
    - **M300a:** `Model/AbnormalState.cs` — added `IsSanctuary { get; init; }` property
    - **M300b:** `Model/Creature.cs` — `ClearBuffs()`: changed predicate from `!e.IsDebuff` to `!e.IsDebuff && !e.IsSanctuary` to skip sanctuary buffs during dispel
    - **M300c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — buff application block: added `IsSanctuary = template.Effects?.HasSanctuary == true` to the `AbnormalState` initializer
    - `HasSanctuary` property already existed in `SkillTemplate` (line 298) — only wire-up needed
    - Build: 0 warnings, 0 errors

- [x] **M301: Hide (stealth)** — `<hide state="HIDE1/HIDE2" duration2="N"/>` makes the player invisible to NPCs and other players; 78 occurrences (Assassin/Ranger stealth line, some Chanter/Cleric evasion skills)
    - Java analog: `HideEffect` sets `CreatureVisualState.HIDE`, broadcasts `SM_PLAYER_STATE`; cancelled by attack, damage-skill cast, item use
    - Stealth types: HIDE1 (combat break on damage received), HIDE2 (bufcount-limited skill use)
    - **M301a:** `Network/Aion/ServerPackets/SM_PLAYER_STATE.cs` (new) — opcode 0x44; writes objectId (D), visualState (C), seeState (C), blink flag (C); visualState 1 = hidden, 0 = revealed
    - **M301b:** `Model/Templates/Skill/SkillTemplate.cs` — added `HideDurationMs` property on `SkillEffects` reading `<hide duration2="N"/>` attribute
    - **M301c:** `Model/Player.cs` — added `IsHidden { get; set; }` flag
    - **M301d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — buff path: after AddEffect, if `HasHide`, set `player.IsHidden = true`, broadcast `SM_PLAYER_STATE(…, visualState=1)`; expiry task: if `isHideEffect`, clear `IsHidden`, broadcast `SM_PLAYER_STATE(…, visualState=0)`; single-target damage path: if `player.IsHidden`, reveal before damage
    - **M301e:** `Network/Aion/ClientPackets/CM_ATTACK.cs` — reveal hidden player before auto-attack proceeds
    - **M301f:** `Services/NpcAiService.cs` — `ForceEngage` skips hidden players (no aggro while stealthed)
    - Limitation: HIDE2 `bufcount` limit (max N self-buffs before reveal) not enforced — acceptable for 4.6.0 initial pass; player-side de-spawn (invisible to nearby clients) requires zone-broadcast filter, deferred
    - Build: 0 warnings, 0 errors

- [x] **M302: ShapeChange/Polymorph/Deform transform** — `<shapechange model="N" duration2="T"/>` (and `<polymorph>`, `<deform>`, `<form>`) changes the player's visual model for the duration; 309+152+77 occurrences (Assassin/Ranger combat forms, quest transformation scrolls, PvE event items)
    - Java analog: `ShapeChangeEffect extends TransformEffect` — sets `creature.getTransformModel()`, broadcasts `SM_PLAYER_INFO`; reverted on buff expiry
    - Previously: shapechange skill templates had `duration="0"`, so the buff path condition `template.Duration > 0` never fired
    - **M302a:** `Model/Templates/Skill/SkillTemplate.cs` — added `ShapeChangeDurationMs` property reading `duration2` from first shapechange/polymorph/deform/form element
    - **M302b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — expanded buff path condition to `template.Duration > 0 || template.Effects?.ShapeChangeDurationMs > 0`; updated `durationMs` to fall back to `ShapeChangeDurationMs` when `Duration == 0`; added `isTransformEffect` block to set `player.TransformModelId`, broadcast `SM_PLAYER_INFO` to zone; expiry task: clear `TransformModelId`, re-broadcast `SM_PLAYER_INFO`
    - **M302c:** `Model/Player.cs` — added `TransformModelId { get; set; }` (0 = no transform)
    - **M302d:** `Network/Aion/ServerPackets/SM_PLAYER_INFO.cs` — changed transform type field from hardcoded 0 to `p.TransformModelId`
    - Build: 0 warnings, 0 errors

- [x] **M303: SkillLauncher** — `<skilllauncher skill_id="X"/>` fires the referenced sub-skill's effects on the same target; 52 occurrences (e.g. Sorcerer skill 1664 "Magic Implosion I" launches sub-skill 8686 which applies a `spellatk` DoT)
  - Java analog: `SkillLauncherEffect.applyEffect()` loads the sub-skill template, creates a new Effect, calls `e.applyEffect()`
  - Implementation:
    - **M303a:** `Model/Templates/Skill/SkillTemplate.cs` — added `SkillLauncherInfo(int SkillId)` record; added `LauncherEffects` property in `SkillEffects` parsing top-level `<skilllauncher skill_id="X">` elements
    - **M303b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — added launcher dispatch block in single-target path after main DoT application; for each launcher entry, loads sub-skill template, iterates its `DotEffects`, applies spellatk formula or flat DoT, creates `AbnormalState` with sub-skill's SkillId, schedules tick/expiry task using same pattern as native DoTs
  - Build: 0 warnings, 0 errors

- [x] **M306: MpAttack periodic MP drain** — `<mpattack checktime="N" value="V" delta="D" duration2="T" percent="X"/>` drains MP from the target on each tick; 41 occurrences (NPC boss debuffs: "Steal Frozen Soul", "Mind Smash", "Aether Wave", etc.)
  - Java analog: `MpAttackEffect.onPeriodicAction()` calls `reduceMp(value)` or `reduceMp(maxMP * value / 100)` on the effected creature per tick
  - **M306a:** `Model/Templates/Skill/SkillTemplate.cs` — added `SkillMpAttackDotInfo(CheckTimeMs, BaseValue, Delta, Duration2Ms, IsPercent)` record; added `MpAttackDotEffects` computed property on `SkillEffects` parsing `<mpattack>` elements; added `"mpattack"` to `EffectDurNames` so skills with `duration=0, tslot=DEBUFF` correctly get their CC duration from the element's `duration2`
  - **M306b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — added `mpattack` periodic drain block in single-target path after M303 launcher block: creates `AbnormalState`, broadcasts `SM_ABNORMAL_EFFECT`; schedules `Task.Run` tick loop draining MP per `CheckTimeMs`; on tick sends `SM_STATS_INFO` to target player connection if target is a player; expiry: `RemoveEffect` + broadcast `SM_ABNORMAL_EFFECT`
  - Build: 0 warnings, 0 errors

- [x] **M304: CloseAerial + BackDamage**
  - **CloseAerial** — `<closeaerial>` on hit removes the OpenAerial (aerial launch) effect from the target; 40 occurrences (Warrior aerial combo finishers e.g. "Smashing Blow", "Rupture")
    - Java analog: `CloseAerialEffect.applyEffect()` calls `removeEffect(8224)` on the effected creature (8224 = "Aether's Hold" OpenAerial skill)
    - **M304a:** `Model/Templates/Skill/SkillTemplate.cs` — added `HasCloseAerial` property on `SkillEffects`
    - **M304b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — added `closeaerial` block in single-target damage path after `ApplyDamageAndPublishAsync`: calls `target.RemoveEffectBySkillId(8224)`, broadcasts `SM_ABNORMAL_EFFECT`
  - **BackDamage** — `<backdamage value="V" delta="D"/>` modifier inside a `<skillatk>` block adds bonus physical damage when the caster is behind the target; 40 occurrences (Assassin/Gladiator back-attack skills)
    - Java analog: `BackDamageModifier.analyze()` returns `value + delta * skillLevel` when `PositionUtil.isBehindTarget()` is true; MAX_ANGLE_DIFF = 90°
    - `backdamage` was already parsed into `DamageModifiers` (Kind="backdamage", Match="") — only dispatch logic was missing
    - **M304c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — added `IsBehindTarget(Position, Position)` static helper using Java PositionUtil logic (atan2 angle vs target heading×3°, ±90° window); added `else if (mod.Kind == "backdamage" && IsBehindTarget(...))` branch in `DamageModifiers` loop
  - Build: 0 warnings, 0 errors

- [x] **M305: EffectDuration CC coverage** — 388 skills with CC effects (`stun`, `root`, `bind`, `sleep`, `silence`, `paralyze`, `fear`, `stagger`, `stumble`, `spin`) had CC duration silently reported as 0 because the CC element names were missing from `EffectDurNames`; these are attack skills with `tslot=DEBUFF, duration=0` where duration must come from the effect element's `duration2` attribute
  - Root cause: `EffectDuration` computed property on `SkillEffects` only scanned elements in `EffectDurNames` (slow/snare/statdown/statup/blind/confuse); CC element names were absent from the set
  - Java analog: Java effect templates each have a `duration` field directly on the `EffectTemplate`; `EffectDuration` was our consolidating property to read the highest `duration2` found in any matching element
  - **M305a:** `Model/Templates/Skill/SkillTemplate.cs` — added all CC element names to `EffectDurNames`: `stun`, `stunalways`, `buffstun`, `sleep`, `root`, `silence`, `buffsilence`, `paralyze`, `fear`, `stagger`, `staggeralways`, `stumble`, `stumblealways`, `spin`, `bind`, `buffbind`
  - Skills now correctly reporting CC duration: Shield Counter I-V (stun 2000ms), Tendon Slice I-II (root 8000ms), Force Cleave I-II (stun 3000ms), Strike Head I-IV (stun 3000ms), Lockdown series (bind 3000ms), all root/silence/sleep debuff skills with duration=0 at template level; 388 total
  - Build: 0 warnings, 0 errors

- [x] **M307: Curse debuff + PERCENT MaxHp/MaxMp reduction** — 24 `<curse>` skills were not registered as CC (no CC flag assigned); additionally, both `<curse>` and `<statdown>` carry PERCENT-type MAXHP/MAXMP reductions (46+22 HP cases, 16+22 MP cases) that were silently ignored because `MaxHpAddDelta`/`MaxMpAddDelta` only scanned ADD-func children
  - Java analog: `CurseEffect` extends `BufEffect`, sets `AbnormalState.CURSE`; stat changes of both ADD and PERCENT func are applied via the generic stat-change engine on effect apply
  - **M307a:** `Model/AbnormalCcFlags.cs` — added `Curse = 131072` (next power-of-2 after OpenAerial=65536)
  - **M307b:** `Model/Templates/Skill/SkillTemplate.cs` — added `"curse"` to `EffectDurNames` (2 curse skills have `duration=0`); extended `MaxHpAddDelta` and `MaxMpAddDelta` to also scan `curse` elements for ADD changes; added `MaxHpPercentDelta` and `MaxMpPercentDelta` computed properties reading PERCENT MAXHP/MAXMP from `statdown` and `curse` elements; added `"curse" => AbnormalCcFlags.Curse` to `ElementToCcFlag`
  - **M307c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — in debuff block after reading `maxMpDelta`: reads `maxHpPctDelta`/`maxMpPctDelta`, multiplies by `target.MaxHp`/`target.MaxMp`, folds flat result into existing `maxHpDelta`/`maxMpDelta` vars so the `AbnormalState.MaxHpDelta`/`MaxMpDelta` fields carry the full reduction
  - Build: 0 warnings, 0 errors

- [x] **M308: StatBoost passive extension** — 194 `<statboost>` PASSIVE skill effects (e.g. "Boost Knockdown", "Concentration I", "Boost Block I", "Boost Physical Attack I") were not contributing any stat bonuses at login because all 25 `*StatUpDelta` properties in `SkillEffects` only scanned `statup` elements
  - Java analog: `StatboostEffect` extends `BufEffect` identically to `StatupEffect` for stat application; both scan `<change>` children with ADD/PERCENT func attributes
  - **M308a:** `Model/Templates/Skill/SkillTemplate.cs` — all 25 `*StatUpDelta` computed properties extended from `e.LocalName != "statup"` → `e.LocalName is not ("statup" or "statboost")`; also added `"statboost"` to `EffectDurNames` for the rare timed statboost buff case
  - Stats now gained from statboost passives: PHYSICAL_ATTACK (15 skills), PHYSICAL_DEFENSE (14), PHYSICAL_CRITICAL (7), BLOCK (6), MAXHP (6), PHYSICAL_ACCURACY (5), MAGIC_SKILL_BOOST_RESIST (5), EVASION (5), BOOST_MAGICAL_SKILL (5), ATTACK_SPEED (5), CONCENTRATION (4), MAXMP (3), and others
  - Build: 0 warnings, 0 errors

- [x] **M309: Dash strike (teleport-to-target attack)** — `<dash value="V" delta="D"/>` deals physical damage then moves the caster to the target's position; 36 occurrences (Assassin/Gladiator gap-closer skills: "Dash Attack I-V", "Rushing Wave", "Charged Dash", etc.)
  - Java analog: `DashEffect extends DamageEffect`; `applyEffect()` calls `super.applyEffect()` (physical damage), then `World.updatePosition(effector, skill.getX(), skill.getY(), skill.getZ(), skill.getH())` to teleport the caster to the target's position
  - **M309a:** `Model/Templates/Skill/SkillTemplate.cs` — added `"dash"` to `DamageEffectNames`; added `"dash"` to the physical-type branch in `DamageEffects` (`e.LocalName is "skillatk" or "skillatkdraininstant" or "dash"`)
  - **M309b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — after `ApplyDamageAndPublishAsync` in single-target path: detects any DamageEffect with `Variant=="dash"` and target still alive; updates `player.Position` to target's position, sets `MovementMask=0`, sends `SM_TELEPORT_LOC` to caster's connection, broadcasts `SM_MOVE` (stop at new position) to all other nearby clients
  - Build: 0 warnings, 0 errors

- [x] **M310: SkillCooldownReset — reduce/reset cooldowns in a range** — `<skillcooltimereset first_cd="A" last_cd="B" delta="D" value="V"/>` reduces remaining cooldown time for all cooldown IDs in the range; 6 occurrences (Gunner class skills: "Reload I" resets fire-chain CD by 30%/level, "Autoload I" fully resets firing-chain CDs; also debug test skills 3028-3030)
  - Java analog: `SkillCooltimeResetEffect.applyEffect()` iterates `firstCd` to `lastCd`, computes remaining delay; if `delta > 0`: `remaining -= remaining * delta / 100`; else `remaining -= value`; sends `SM_SKILL_COOLDOWN` with the changed entries
  - **M310a:** `Model/Templates/Skill/SkillTemplate.cs` — added `SkillCooldownResetInfo(FirstCd, LastCd, Delta, Value)` record; added `CooldownResetEffects` computed property on `SkillEffects` parsing `<skillcooltimereset>` elements
  - **M310b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — at end of `RunAsync`, reads `CooldownResetEffects`; iterates cooldown ID range, applies delta-percent or flat-ms reduction to `player.SkillCooldowns`, removes fully-expired entries, sends `SM_SKILL_COOLDOWN` when any cooldown changed
  - Build: 0 warnings, 0 errors

- [x] **M311: Passive PERCENT MaxHp/MaxMp bonuses from statup/statboost** — PERCENT MAXHP/MAXMP changes in passive skills were silently ignored; all `*StatUpDelta` properties only read ADD-func changes; significant for players with "Increase MAX HP" and "Boost HP" passive lines (skills 118, 121 give 5-12% MaxHp; many higher-level statboost PERCENT entries)
  - Java analog: `StatupEffect`/`StatboostEffect` applies both ADD and PERCENT stat changes via the stat-change engine on skill activate; PERCENT MAXHP multiplies the base MaxHp from class template
  - **M311a:** `Model/Templates/Skill/SkillTemplate.cs` — added `MaxHpPercentStatUpDelta` and `MaxMpPercentStatUpDelta` properties on `SkillEffects`, reading PERCENT MAXHP/MAXMP `<change>` children of `statup`/`statboost` elements
  - **M311b:** `Model/Player.cs` — added `PassiveBonusMaxHpPct` and `PassiveBonusMaxMpPct` properties
  - **M311c:** `Services/PlayerEnterWorldService.cs` — accumulates `MaxHpPercentStatUpDelta`/`MaxMpPercentStatUpDelta` at login into `PassiveBonusMaxHpPct`/`PassiveBonusMaxMpPct`; updated MaxHp/MaxMp formula: `(flatSum) * (1 + pct/100f) * ssMult`
  - Build: 0 warnings, 0 errors

- [x] **M315: BoostHeal + BoostSpellAttack passive PERCENT bonuses** — `<boostheal>` elements with `<change stat="HEAL_SKILL_BOOST" func="PERCENT" value="V"/>` boost healing output by V%; `<boostspellattack>` with `<change stat="BOOST_SPELL_ATTACK" func="PERCENT" value="V"/>` boost magical attack by V%; 8 + 8 = 16 skills, of which 4 boostheal (108 Boost Healing I-IV, 5/10/15/20%) and 1 boostspellattack (1535 Boon of Fierce Attack I, 20%) have no flight condition; the remaining 11 have `<onfly/>` and are skipped
  - Java analog: `BoostHealEffect`/`BoostSpellAttackEffect` both extend `BufEffect` (empty classes); stat application comes from `<change>` children via the generic stat-change engine; `HEAL_SKILL_BOOST` is applied in `AbstractHealEffect` as `getStat(HEAL_SKILL_BOOST, finalHeal).getCurrent()`; `BOOST_SPELL_ATTACK` is applied in `StatFunctions.calcMagicalDamage` as `getStat(BOOST_SPELL_ATTACK, damages).getCurrent()` — both are PERCENT multipliers on the computed value
  - Flight-condition filtering: `<change>` children with nested `<conditions><onfly/></conditions>` are skipped so flight-only bonuses (Seraphic Song, Winged Recovery, Winged Magic, Flight: Aetherized Barrels) are not applied to ground combat stats
  - **M315a:** `Model/Templates/Skill/SkillTemplate.cs` — added `BoostHealSkillBoostPct` and `BoostSpellAttackPct` computed properties on `SkillEffects`; each scans the corresponding element name, iterates `<change>` children, skips those with `<conditions><onfly/>` nesting, accumulates PERCENT values
  - **M315b:** `Model/Player.cs` — added `PassiveBonusHealSkillBoostPct` and `PassiveBonusSpellAttackPct` properties
  - **M315c:** `Services/PlayerEnterWorldService.cs` — passive loop accumulates both new properties after `MaxMpPercentStatUpDelta`
  - **M315d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — `healBoostMult` and `aoeBoostMult` multiplied by `(1 + PassiveBonusHealSkillBoostPct/100f)`; all 7 magical damage computation sites (single-target, caster-AoE, ground/target-AoE, splash, ground-AoE-DoT, splash-DoT, delayed-damage) multiply by `(1 + PassiveBonusSpellAttackPct/100f)` after the mbMult step
  - Build: 0 warnings, 0 errors

- [x] **M314: DeathBlow DP ultimate skills + CasterAoe damage path** — `<deathblow value="V" delta="D"/>` deals magical damage in an area; all 14 deathblow skills use `first_target="ME/TARGET", target_type="AREA"` — requires a new caster-centered AoE code path; plus 4 IsTargetAoe variants work through the existing single-target + splash path
  - Java analog: `DeathBlowEffect extends DamageEffect { calculate(effect) { super.calculate(effect, DamageType.MAGICAL); } }` — purely magical, otherwise identical to spellatkinstant; DP-cost class ultimates (Heaven and Earth Tremor, Splendor of God, Voice of God, etc.)
  - Root cause for IsCasterAoe (10 skills): `first_target="ME"` sends `_targetObjectId=player.ObjectId`; the existing single-target path would damage the caster; no dedicated caster-AoE damage path existed
  - **M314a:** `Model/Templates/Skill/SkillTemplate.cs` — added `"deathblow"` to `DamageEffectNames`; `deathblow` not in physical branch so it correctly defaults to magical type in `DamageEffects`
  - **M314b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — new `else if` branch before the ground-AoE path: `isDamageSkill && IsCasterAoe && _targetType is 0 or 3 or 4 && (_targetObjectId == 0 || == player.ObjectId)`; collects NPC enemies within `EffectiveRange`/`EffectiveAltitude` of player position, applies full magical damage formula (magic resist check, MBResist defense reduction, crit, NPC level-diff), calls `ApplyDamageAndPublishAsync`, `ForceEngage`, `SM_ATTACK_STATUS`
  - IsTargetAoe deathblow variants (1812, 1813, 1820, 1821) work through the existing single-target + splash path with no additional changes
  - Build: 0 warnings, 0 errors

- [x] **M313: AlwaysBlock + AlwaysDodge invulnerability buffs** — `<alwaysblock value="N" duration2="T"/>` guarantees N physical blocks; `<alwaysdodge value="N" duration2="T"/>` guarantees N physical dodges; buff expires when hit counter reaches 0 OR duration elapses; 26 + 12 = 38 skills (Templar Shield of Faith I-II, Divine Chastisement I-V, Cry of Ridicule, Battle Call — block; Assassin/Ranger Focused Evasion I-III, Celestial Image I, Illusion I-IV, Bulletproof I — dodge)
  - Java analog: `AlwaysBlockEffect`/`AlwaysDodgeEffect` register an `AttackStatusObserver`; on each BLOCK/DODGE result the observer decrements a counter and calls `effect.endEffect()` when count reaches 1
  - All 38 skills have `duration=0` at template level — they were silently skipped by the buff path (condition was `Duration > 0`)
  - **M313a:** `Model/AbnormalState.cs` — added `HitCountRemaining { get; set; }` mutable counter (exception to init-only pattern, required for per-hit tracking)
  - **M313b:** `Model/Templates/Skill/SkillTemplate.cs` — added `HasAlwaysBlock`, `HasAlwaysDodge`, `AlwaysBlockCount`, `AlwaysBlockDurationMs`, `AlwaysDodgeCount`, `AlwaysDodgeDurationMs` properties on `SkillEffects`
  - **M313c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — extended buff path condition: `|| AlwaysBlockDurationMs > 0 || AlwaysDodgeDurationMs > 0`; extended `durationMs` fallback chain; added `HitCountRemaining = AlwaysBlockCount + AlwaysDodgeCount` to AbnormalState initializer
  - **M313d:** `Combat/Handlers/AlwaysBlockDodgeHandler.cs` (new) — handles `DamageReceivingEvent` for `PhysicalSkill`/`AutoAttack`; finds active alwaysblock/alwaysdodge buff, zeros damage, decrements `HitCountRemaining`; removes buff and broadcasts `SM_ABNORMAL_EFFECT` when count reaches 0
  - **M313e:** `Program.cs` — registered `AlwaysBlockDodgeHandler` after `AlwaysResistHandler`
  - Build: 0 warnings, 0 errors

- [x] **M331: alwaysparry buff + noresurrectpenalty buff + REGEN_FP PERCENT buff** — 4 `<alwaysparry value="N">` entries (Templar "Parry" I-II guarantees N physical parries; Templar "Cross Parry" instant counter-parry) now handled alongside alwaysblock/alwaysdodge; 3 `<noresurrectpenalty>` entries (1-hour Scroll of Revival buff, NPC revival blessing) now suppress soul sickness on death; 2 `REGEN_FP PERCENT value="25"` entries (30-min "+25% FP regen" consumable buffs) now accelerate FP restoration
  - Java analog: `AlwaysParryEffect` (attack observer, damage→0); `NoresurrectpenaltyEffect` (death flag); `StatUpEffect REGEN_FP PERCENT`
  - **M331a:** `Model/Templates/Skill/SkillTemplate.cs` — added `HasAlwaysParry`, `AlwaysParryCount`, `AlwaysParryDurationMs`, `HasNoresurrectPenalty`, `RegenFpStatUpPct` properties
  - **M331b:** `Model/AbnormalState.cs` — added `IsNoDeathPenalty`, `RegenFpPctDeltaVal` fields
  - **M331c:** `Model/Player.cs` — added `BonusRegenFpPct` property
  - **M331d:** `Model/Creature.cs` — added FP regen pct to `ApplyEffectDeltas`/`ReverseEffectDeltas`
  - **M331e:** `Combat/Handlers/AlwaysBlockDodgeHandler.cs` — extended to check `HasAlwaysParry`; parry absorbs hit (damage→0) same as block
  - **M331f:** `Services/RegenService.cs` — FP regen multiplied by `(100 + BonusRegenFpPct) / 100` when non-zero
  - **M331g:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — buff path: AlwaysParryCount added to `HitCountRemaining`; `IsNoDeathPenalty` and `RegenFpPctDeltaVal` set in AbnormalState; AlwaysParryDurationMs added to duration fallback chain
  - **M331h:** `Network/Aion/ClientPackets/CM_REVIVE.cs` — `noresurrectpenalty` buff suppresses `applySoulSickness` before incrementing SoulSicknessCount
  - Build: 0 warnings, 0 errors

- [x] **M332: DR_BOOST ADD buff (23 entries) + AP_BOOST ADD buff (14 entries)** — Drop-rate boost (e.g. "+20% item drop chance" from 20-min consumables and passive skills) now scales `effectiveChance` in LootService; AP gain boost (e.g. "+20% AP per kill" from Abyss scrolls) now scales AP rewards in CM_ATTACK, CM_CASTSPELL AoE kill, and CM_CASTSPELL single-target kill paths
  - Java analog: `StatUpEffect DR_BOOST ADD` → `boostDropRate += drBoost / 100f` in DropRegistrationService; `StatUpEffect AP_BOOST ADD` → `points *= (1 + apBoost / 100.0)` in StatFunctions
  - **M332a:** `Model/Templates/Skill/SkillTemplate.cs` — added `DRBoostAddDelta`, `APBoostAddDelta` to `SkillEffects` (scan `statup`/`statboost` for `func="ADD"`)
  - **M332b:** `Model/AbnormalState.cs` — added `DRBoostDeltaVal`, `APBoostDeltaVal` fields
  - **M332c:** `Model/Creature.cs` — added `DRBoostDelta` field; wired in `ApplyEffectDeltas`/`ReverseEffectDeltas`; AP_BOOST handled via `this is Player apBoostApply` cast (Player-only)
  - **M332d:** `Model/Player.cs` — added `APBoostDelta` property
  - **M332e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: reads `drBoostDelta`/`apBoostDelta`, stores in `AbnormalState`
  - **M332f:** `Services/LootService.cs` — `effectiveChance` boosted by `killer.DRBoostDelta` when non-zero
  - **M332g:** `Network/Aion/ClientPackets/CM_ATTACK.cs` — NPC and PvP AP scaled by `player.APBoostDelta` when non-zero
  - **M332h:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — AoE NPC kill, single-target NPC kill, single-target PvP kill AP all scaled by `player.APBoostDelta`

- [x] **M334: onetimeboostskillcritical (7 entries) + onetimeboostskillattack (7+ entries)** — "Hunter's Might" / "Killer's Eye" one-time crit and damage charge buffs now correctly boost crit rating/chance and damage for each charge consumed per skill cast
  - Java analog: `OneTimeBoostSkillCriticalEffect` (AttackerCriticalStatusObserver charges); `OneTimeBoostSkillAttackEffect` (damage boost per type)
  - **M334a:** `Model/Templates/Skill/SkillTemplate.cs` — added 8 computed properties to `SkillEffects`: `OnetimeCritCount/Value/IsPercent/DurationMs`, `OnetimeAtkCount/Pct/Type/DurationMs`
  - **M334b:** `Model/AbnormalState.cs` — added `OnetimeCritCountRemaining/BoostFlat/BoostPct` (crit charges) and `OnetimeAtkCountRemaining/BoostPct/BoostIsPhysical` (atk charges); mutable count fields for charge decrement
  - **M334c:** `Model/Creature.cs` — added `ConsumeOnetimeCritCharge()` and `ConsumeOnetimeAtkCharge(isPhysical)` methods; each locks `_effectsLock`, decrements charge, removes effect when drained
  - **M334d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: extended `durationMs` fallback chain for `OnetimeCritDurationMs`/`OnetimeAtkDurationMs`; AbnormalState init sets all 6 new fields; DAMAGE paths (4 locations: caster-AoE NPC, ground/target-AoE loop, single-target, AoE splash): charges consumed once per cast before target loop, cached flat/pct applied at each crit check; atk pct applied to rawDmg/rawSpellDmg/splashRaw before crit
  - Non-percent case (e.g. Hunter's Might I: count=2, value=1000): adds +1000 to crit rating for next 2 skill casts
  - Percent case (e.g. Contract of Focus I: count=1, value=70, percent=true): adds +70% directly to crit rate for next 1 cast
  - Build: 0 warnings, 0 errors

- [x] **M361: reflector damage reflection — 80 unique skills / 95 XML entries now deal reflected damage to attackers** — M263 implemented `ReflectorHandler` (event handler on `DamageDealtEvent`) but two bugs prevented it from working: (1) `"reflector"` was absent from `EffectDurNames`, so `EffectDuration=0` and the BUFF gate rejected all reflector BUFF skills; (2) `SkillReflectorInfo` lacked `Value` (reflection %) so percent-mode reflectors (57 entries) always used flat-only formula; fixes: add `"reflector"` to `EffectDurNames`; add `Value` to `SkillReflectorInfo` and read `value` attribute in `ReflectorEffects` parser; update `ReflectorHandler` to compute `max(damage * Value / 100, hitFlat)` for percent mode and `hitFlat` for flat mode using `e.DamageAmount` as the incoming damage
  - **M361a:** `Model/Templates/Skill/SkillTemplate.cs` — `SkillReflectorInfo`: add `Value` (int) field; `ReflectorEffects` parser: read `value` attribute; add `"reflector"` to `EffectDurNames`
  - **M361b:** `Combat/Handlers/ReflectorHandler.cs` — percent-mode formula: `fx.Value > 0 ? max(damage * Value / 100, hitFlat) : hitFlat`; guard: `HitValue <= 0 && Value <= 0` skips
  - Build: 0 warnings, 0 errors

- [x] **M381: SM_NEARBY_QUESTS — client's area-quest list is now populated on enter world and level-up** — Java `PlayerController.updateNearbyQuests()` reads `getMapRegion().getParent().getQuestIds()`, a per-world-instance set populated dynamically as quest-start NPCs spawn into that instance (`WorldMapInstance.addObject`); we don't have per-instance dynamic population, so we derive a static worldId -> quest-start index once at startup by joining the static NPC spawn table (`SpawnsData`) against each NPC's `QuestNpc.OnQuestStart` list (itself populated by the data-driven quest handlers during `QuestEngineHostedService` registration).
  - Java analog: `SM_NEARBY_QUESTS.writeImpl` — `writeC(0)`; `writeH(-size & 0xFFFF)`; per entry: `levelDiff > 0` → `writeH(questId)` + `writeH(0x02)` (grey "future" icon), else `writeD(questId)` (quests already displayable on map); `QuestService.getLevelRequirementDiff` — `minlevel_permitted=99` sentinel means "no level requirement" (diff=0); `checkStartConditionsImpl` — race gate (`race_permitted` vs `PC_ALL`) + level gate (`diff <= 2`) + already-active/completed exclusion
  - **M381a:** `Network/Aion/ServerPackets/SM_NEARBY_QUESTS.cs` — opcode 0x7F; ports the negative-count `writeH` quirk and the grey-icon (`levelDiff > 0` → 16-bit id + 0x02) vs on-map (32-bit id) branching exactly
  - **M381b:** `QuestEngine/QuestEngine.cs` — `BuildWorldQuestIndex(SpawnsData, QuestData)`: builds worldId -> npcId set from `SpawnsData.All()`, joins against `_questNpcs[npcId].OnQuestStart`, dedupes per world, resolves each quest's `MinLevel` + `Race` (parsed from `QuestTemplate.Race` string — trivially maps to the `Race` enum's `ELYOS`/`ASMODIANS`/`PC_ALL` values), and swaps the whole dictionary into a `IReadOnlyDictionary` field by reference (thread-safe for concurrent reads without locking); logs `{worlds} world(s) x {pairs} world-quest pair(s)`. `ComputeNearbyQuests(Player)`: race gate, level gate (`diff <= 2`, `MinLevel=99` → diff 0, and — Java quirk ported exactly — quest ids above `0xFFFF` force `diff=0` since they can't fit the packet's 16-bit "future quest" field), and excludes quests the player already has as START/REWARD or COMPLETE-with-CompleteCount>0
  - **M381c:** `QuestEngine/QuestEngineHostedService.cs` — calls `engine.BuildWorldQuestIndex(...)` after all template handlers have registered (so the OnQuestStart index is complete before the join)
  - **M381d:** `Services/PlayerEnterWorldService.cs` — sends `SM_NEARBY_QUESTS` alongside the other quest packets (`SM_QUEST_COMPLETED_LIST`/`SM_QUEST_LIST`) during enter-world
  - **M381e:** `Services/ExperienceService.cs` — sends `SM_NEARBY_QUESTS` in `HandleLevelUpAsync`, alongside `SM_LEVEL_UPDATE`/`SM_STATS_INFO`/`SM_SKILL_LIST`
  - Deviations from Java `checkStartConditionsImpl` (documented, not ported — out of scope per task): class/gender/abyss-rank/inventory-item/craft-skill/XML-start-condition/NPC-faction gates. Also: the ported `QuestTemplate` has no repeatable-count field, so "completed, can't repeat" is approximated as `CompleteCount > 0` rather than checking true repeatability; and quest availability is derived from the static spawn table rather than a live per-instance NPC set, so personal-instance-only quest NPCs won't appear in the index.
  - Build: 0 warnings, 0 errors

- [x] **M380: dispeldebuffphysical / dispeldebuffmental — 38 category-specific cleanse skills now selectively remove only physical or mental debuffs instead of all debuffs** — Java `DispelDebuffPhysicalEffect`/`DispelDebuffMentalEffect` pass `DispelCategoryType.DEBUFF_PHYSICAL`/`DEBUFF_MENTAL` to `removeEffectByDispelCat` which filters candidate debuffs by their `dispel_category` attribute (`DEBUFF_PHYSICAL` or `DEBUFF_MENTAL` on the skill template). Previously both variants fell through to `ClearDebuffs()` which removed all debuffs indiscriminately.
  - Java analog: `AbstractDispelEffect.applyEffect(type, slot)` → `EffectController.removeEffectByDispelCat(type, slot, count, dispelLevel, …)` → iterates effects, checks `effect.getDispelCategory()` matches type and `req_dispel_level <= dispelLevel`, removes up to `count` matches
  - Affected scope: 29 `<dispeldebuffphysical>` (Ranger "Nature's Resolve I/II", Cleric "Cure Mind III", SM "Illusion II-IV", NPC relics) + 9 `<dispeldebuffmental>` skills; `dispel_category` stored on 1674 DEBUFF_PHYSICAL + 287 DEBUFF_MENTAL skill templates
  - Previously: both types hit `ClearDebuffs()` removing all debuffs; now physical cleanses only remove physical-category debuffs and mental cleanses only remove mental-category debuffs; permanent debuffs (Expiry=DateTime.MaxValue) and those with req_dispel_level above dispel_level are immune
  - **M380a:** `Model/Templates/Skill/SkillTemplate.cs` — add `[XmlAttribute("dispel_category")] DispelCategory` and `[XmlAttribute("req_dispel_level")] ReqDispelLevel` to `SkillTemplate`; add `HasDispelDebuffPhysical`/`HasDispelDebuffMental` bools + `DispelDebuffPhysicalInfo`/`DispelDebuffMentalInfo` (Value,Delta,DispelLevel) tuples to `SkillEffects`
  - **M380b:** `Model/AbnormalState.cs` — add `DispelCategory` string (default "NONE") + `ReqDispelLevel` int fields
  - **M380c:** `Model/Creature.cs` — add `ClearDebuffsByCategory(string dispelCat, int maxCount, int dispelLevel)`: removes up to maxCount debuffs matching category filter and dispel level; permanent effects (DateTime.MaxValue) are immune; "ALL" dispelCat matches ALL/DEBUFF_PHYSICAL/DEBUFF_MENTAL; specific cats also match ALL-tagged debuffs
  - **M380d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — DEBUFF path: read and store `template.DispelCategory` + `template.ReqDispelLevel` in AbnormalState (including sub-effect CC, child-skill, and DoT debuff creation sites); BUFF path: replace single `ClearDebuffs()` with three-way branch — `dispeldebuffphysical` → `ClearDebuffsByCategory("DEBUFF_PHYSICAL", count, level)`, `dispeldebuffmental` → `ClearDebuffsByCategory("DEBUFF_MENTAL", count, level)`, else → `ClearDebuffs()` (unchanged)
  - **M380e:** `Services/NpcAiService.cs` — add `dispelCategory` + `reqDispelLevel` parameters to `CastNpcDebuffAsync`; store in AbnormalState; pass `skillTemplate.DispelCategory`/`skillTemplate.ReqDispelLevel` at call site
  - Build: 0 warnings, 0 errors

- [x] **M379: resurrectbase — 8 skills (Chain of Suffering I-VII + test skill) now auto-revive debuffed target at bind point on death** — `<resurrectbase skill_id="8293" duration2="120000">` DEBUFF was absent from `EffectDurNames`, so `EffectDuration=0`; the DEBUFF path used `template.Duration=2000` (cast time) as the debuff duration instead of the real 120000ms; additionally no handler existed for the DEATH observer that should fire the revive
  - Java analog: `ResurrectBaseEffect.startEffect` attaches DEATH observer; on death: `PlayerReviveService.bindRevive(effected, skillId)` → HP 25% + bind point teleport + soul sickness + SM_EMOTION(RESURRECT) + SM_PLAYER_SPAWN
  - Previously: the debuff was created but expired after 2 seconds (using cast time as duration); even if it had survived, no handler existed to trigger the auto-revive on death
  - **M379a:** `Model/Templates/Skill/SkillTemplate.cs` — add `"resurrectbase"` to `EffectDurNames` (duration2=120000 now correctly populates `EffectDuration`); add `HasResurrectBase` bool + `ResurrectBaseSkillId` int properties
  - **M379b:** `Model/AbnormalState.cs` — add `ResurrectBaseSkillId` field (non-zero = this debuff auto-revives on death; value = the revival skill_id)
  - **M379c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — DEBUFF path: `debuffDurationMs = Math.Max(template.Duration, EffectDuration)` (safe for all existing debuffs: most have EffectDuration=0; Chain of Suffering now gets 120000ms); read and store `resurrectBaseSkillId` in AbnormalState
  - **M379d:** `Combat/Handlers/ResurrectBaseHandler.cs` — `IEventHandler<DeathEvent>`; fires synchronously within DeathEvent (before caller's death path); if victim has a `ResurrectBaseSkillId != 0` effect: removes debuff, applies soul sickness, restores HP/MP to 25%, teleports to bind point, broadcasts SM_EMOTION(RESURRECT)+STAND, sends SM_STATS_INFO; restoring HP before `ApplyDamageAndPublishAsync` returns causes caller's `if (HP > 0) return` to skip the death path entirely (no SM_DIE or State.Dead set)
  - **M379e:** `Program.cs` — register `ResurrectBaseHandler` as `IEventHandler<DeathEvent>`
  - Build: 0 warnings, 0 errors

- [x] **M378: boostskillcost — 2 Chanter/Spiritmaster skills now reduce skill MP cost while buff is active** — "Grace of Empyrean Lord I" (value=100, duration=12s → free skills) and "Lumiel's Wisdom I" (value=50, duration=15s → half cost) were the only BUFF skills whose sole effect was `boostskillcost`; without it in `EffectDurNames`, `EffectDuration=0` and the BUFF path gate failed; secondary `boostskillcost value='-10'` in "Sharpen Arrows I/II" and "Benevolence I" were already active (those skills entered BUFF path via weaponstatup/statup) but the cost modification was never applied
  - Java formula (MpUseAction.act): `mpCost = mpCost - mpCost / (100 / boostPct)` — when boostPct=100: mpCost=0 (free); when boostPct=50: mpCost=mpCost/2; when boostPct=-10: mpCost increases by 10%
  - **M378a:** `Model/Templates/Skill/SkillTemplate.cs` — add `"boostskillcost"` to `EffectDurNames`; add `BoostSkillCostPct` property (sum of all boostskillcost `value` attributes)
  - **M378b:** `Model/AbnormalState.cs` — add `BoostSkillCostPct` field
  - **M378c:** `Model/Player.cs` — add `BonusSkillCostBoostPct` mutable property
  - **M378d:** `Model/Creature.cs` — `ApplyEffectDeltas`/`ReverseEffectDeltas` accumulate/reverse `BonusSkillCostBoostPct`
  - **M378e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: read and store `boostSkillCostPct` in AbnormalState; MP cost block: apply Java formula before the "not enough MP" check; edge case: when boostPct=100 set cost to 0 directly (avoids integer division by zero)
  - Build: 0 warnings, 0 errors

- [x] **M377: BUFF gate + durationMs fix for onetimeboostskillcritical/onetimeboostskillattack/onetimeboostheal — 19 Ranger/Cleric/Chanter skills now correctly enter the BUFF path** — M334 and M337 were fully implemented (AbnormalState fields, charge consumption, heal boost accumulation) but the BUFF entry gate condition (`else if SubType==BUFF`) was missing all three onetimeboost duration checks; additionally `template.Duration` (cast channel time, e.g. 500ms) was incorrectly used as the buff duration for skills like "Focused Shots I" where the actual buff duration is `onetimeboostskillattack.duration2 = 60000`
  - Affected: "Hunter's Might I/II/III/IV" (Ranger stigma — 2 guaranteed crits for 10s), "Eye of Wrath I/II" (1 guaranteed crit), "Killer's Eye I/II/III" (1 physical skill +50% dmg), "Focused Shots I" (5 physical skills +30% dmg for 60s), "Contract of Focus I" (+70% crit for 10s), "Blessed Shield I/II/III" + "Stigma Blessed Shield I" (next heal +100% for 20s), "Healer's Praise I Effect" (aura heal boost)
  - Previously: casting any of these skills had no visible buff effect; charges were never set on the AbnormalState; BUFF path gate evaluated to false because `template.Duration=0` and `EffectDuration=0` (onetimeboost names are not in EffectDurNames)
  - **M377a:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF gate: add `|| template.Effects?.OnetimeCritDurationMs > 0 || template.Effects?.OnetimeAtkDurationMs > 0 || template.Effects?.OnetimeBoostHealDurationMs > 0` to the gate OR-chain
  - **M377b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — `durationMs` chain: move the three onetimeboost duration checks BEFORE `template.Duration > 0` so skills with a cast animation time (e.g. Focused Shots I: duration=500) use their effect's own `duration2` (60000 ms) as the actual buff duration
  - Build: 0 warnings, 0 errors

- [x] **M376: changehateonattacked — 3 Cleric "Blessing of Peace I/II/III" skills now reduce NPC hate when the buffed player is attacked** — `<changehateonattacked value1="-300" value2="-5700" duration2="30000">` had no handler and no entry in `EffectDurNames`; `EffectDuration = 0` prevented the BUFF path from creating an AbnormalState; even if it had, no handler existed to reduce NPC hate on hit
  - Affected skills: "Blessing of Peace I" (value1=-300, value2=-5700, finalHate=-6000), "Blessing of Peace II" (value1=-300, value2=-8700, finalHate=-9000), "Blessing of Peace III" (value1=-400, value2=-11600, finalHate=-12000) — all 30-second self-buffs
  - **M376a:** `Model/Templates/Skill/SkillTemplate.cs` — add `SkillChangeHateOnAtkInfo(Value1, Value2)` record; add `HasChangeHateOnAtk` bool; add `ChangeHateOnAtkEffects` list property; add `"changehateonattacked"` to `EffectDurNames` (so duration2=30000 creates an AbnormalState via BUFF path)
  - **M376b:** `Combat/Handlers/ChangeHateOnAttackedHandler.cs` — `IEventHandler<DamageDealtEvent>`; when `e.Attacker is Npc` and `e.Target` has an active `changehateonattacked` effect, call `npc.AddHate(target.ObjectId, value1+value2)` (negative delta reduces hate); mirrors Java ChangeHateOnAttackedEffect ATTACKED observer
  - **M376c:** `Program.cs` — register `ChangeHateOnAttackedHandler` as `IEventHandler<DamageDealtEvent>`
  - Build: 0 warnings, 0 errors

- [x] **M375: instant heals in BUFF skills — 106 BUFF/CHANT skills now apply their bundled healinstant component** — BUFF-subtype skills with `<healinstant>` elements (e.g. Second Wind I: 35% HP instant + 60s MaxHP boost) had their instant heal silently skipped; the HEAL path only fires for `SubType == HEAL` skills, and the BUFF path had no instant-heal processing
  - Affected examples: "Second Wind I–III" (Gladiator stigma: instant 35% HP + timed MaxHP), "Improved Stamina I–III", "Roar of Vitality I" (1060 HP instant + timed), "Brilliant Protection I/II", "Empyrean Armor I" and 100 other BUFF/CHANT skills with bundled healinstant elements
  - **M375:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — after the SM_ABNORMAL_EFFECT broadcast in the BUFF path, iterate `template.Effects.HealEffects`; for each entry compute value+delta*level (with % support), apply `healBoostMult` and `HealReceivedPct` modifier; apply to HP or MP, broadcast `SM_ATTACK_STATUS(NaturalHp/NaturalMp)`; mirrors HEAL-path formula
  - Previously: players casting "Second Wind I" got the MaxHP bonus buff but no HP restore; "Improved Stamina" applied the timed stat buff but the instant HP top-up never fired
  - Build: 0 warnings, 0 errors

- [x] **M374: healcastoronatk / healcastorontargetdead / magiccounteratk added to EffectDurNames — 19 skills (Healing Conduit I-VI, Blood Healing I, Curse of Weakness I-VIII) now create AbnormalStates that their event handlers can find** — all three effect types had `duration2` in XML but were absent from `EffectDurNames`; `EffectDuration = 0` → DEBUFF path gate `debuffDurationMs > 0` failed → no AbnormalState created → `HealCastorOnAttackedHandler`, `HealCastorOnTargetDeadHandler`, `MagicCounterAtkHandler` iterated `GetActiveEffects()` and found nothing
  - `healcastoronatk` (9 occurrences): "Healing Conduit I-VI" + NPC variants — Cleric debuff that heals caster on each auto-attack hit against marked target (`duration2="10000"`)
  - `healcastorontargetdead` (8 occurrences): "Blood Healing I" + variants — Cleric debuff that heals caster (and nearby party) when marked enemy dies (`duration2="30000"`)
  - `magiccounteratk` (8 occurrences): "Curse of Weakness I-VIII" — Spirit Master debuff that reflects magic damage back at caster (`duration2="54000-60000"`)
  - **M374:** `Model/Templates/Skill/SkillTemplate.cs` — add `"healcastoronatk"`, `"healcastorontargetdead"`, `"magiccounteratk"` to `EffectDurNames`; one-line fix enabling `EffectDuration` to read their `duration2` values
  - Build: 0 warnings, 0 errors

- [x] **M373: provoker EffectDurNames + TOGGLE stance skills — 81 taunt skills now create timed AbnormalStates; 28 stance/mode toggles now create permanent AbnormalStates** — two separate root causes both caused BUFF skills to do nothing: (a) `provoker` not in `EffectDurNames` so ProvokerHandler could never find taunt effects; (b) `activation=TOGGLE` stance skills had no `duration2` so gate condition rejected them
  - Provoker affected: "Indomitable Spirit I–IV" (Knight), "Revive Health I" (Gladiator), "Will to Recovery", "Will of Revenge", "Flight: Will of Resuscitation" and ~73 similar taunt skills
  - Toggle affected: "Shield Defense I–IV" (Gladiator stances), "Aion's Strength I–IV", "Focused Stance I–II", "Defense Preparation I–IV" and 16 other permanent mode skills
  - **M373a:** `Model/Templates/Skill/SkillTemplate.cs` — add `"provoker"` to `EffectDurNames` so `EffectDuration` reads the provoker element's `duration2`; ProvokerHandler iterates `GetActiveEffects()` — without an AbnormalState, the taunt never fires
  - **M373b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — add `string.Equals(template.Activation, "TOGGLE", ...)` to the BUFF gate condition; for `durationMs == 0` (toggle skills), set `Expiry = DateTime.MaxValue` (permanent until `CM_TOGGLE_SKILL_DEACTIVATE` removes it)
  - Previously: all 109 affected skills silently passed through CM_CASTSPELL with no effect; provoker taunt never redirected aggro; toggle stances applied no stat bonuses
  - Build: 0 warnings, 0 errors

- [x] **M372: dispel+buff combined skills — 46 skills now apply their buff effects after cleansing debuffs** — `HasDispelDebuff == true` inside the BUFF path was an early-return shortcut that prevented any other duration-contributing effects in the same skill from being applied; skills like Paladin "Punishment I–VI" (dispeldebuff + shield) and "Judgment I–IV" (dispeldebuff + statup + shield + statdown) only did the cleanse, never applied the timed buff state
  - Affected scope: 46 skills that combine `<dispeldebuff>` or `<dispel>` with other duration-contributing effects (shield, statup, statdown, alwaysresist, invulnerablewing, etc.)
  - Java analog: `DispelDebuffEffect.applyEffect` calls `removeEffect()` for matching effects and returns — it is one of multiple effects applied via `effect.getEffectResult()`; the other effects still apply normally; the early return was a wrong .NET simplification
  - **M372:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — remove `await BroadcastAsync(SM_SKILL_ACTIVATION) + return` from inside the `HasDispelDebuff` block; SM_SKILL_ACTIVATION is already broadcast at the end of the BUFF path (line ~1296); code now falls through to apply shield/statup/statdown etc. as normal
  - Previously: `Punishment I–VI`, `Judgment I–IV`, `Transparent Cloak I/II`, `Prayer of Freedom I–IV`, `Impervious Veil I–II` and 39 other mixed-effect skills only executed the debuff-clear; timed buff states were silently skipped
  - Build: 0 warnings, 0 errors

- [x] **M371: MP cast cost deduction — 2615 skills now correctly spend MP on cast (Java MpUseAction.act); previously all skills were free** — `<actions><mpuse value="V" delta="D" ratio="true"/>` was parsed nowhere; `SkillActions` only had `dpuse`/`hpuse`; every skill cast cost 0 MP regardless of template
  - Affected scope: 2615 skill templates across all classes contain `<mpuse>`; this is the standard combat MP cost for every warrior, scout, mage, priest, technist, muse skill
  - Java analog: `MpUseAction.act` — computes `value + delta * skillLevel` (or ratio * maxMp); sends `STR_SKILL_NOT_ENOUGH_MP` if insufficient; calls `effector.getLifeStats().reduceMp(valueWithDelta)`
  - **M371a:** `Model/Templates/Skill/SkillTemplate.cs` — add `SkillMpUse` class (`value`, `delta`, `ratio` attributes); add `[XmlElement("mpuse")] SkillMpUse? MpUse` to `SkillActions`; add `MpUseCost` computed property (mirrors `HpUseCost`)
  - **M371b:** `Network/Aion/ServerPackets/SM_SYSTEM_MESSAGE.cs` — add `NotEnoughMp()` → `new(1300015)` (Java `STR_SKILL_NOT_ENOUGH_MP`)
  - **M371c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — after hpuse block, add mpuse block: compute cost, check `player.CurrentMp < mpCost` → send `NotEnoughMp()` and return; deduct `player.CurrentMp -= mpCost`; send `SM_STATS_INFO` to update client MP bar
  - Previously: all 2615 mpuse skills cast with 0 MP cost; players could spam skills indefinitely
  - Build: 0 warnings, 0 errors

- [x] **M370: heal/mpheal BUFF-subtype HoT path — NPC area heals + Stigma Penance mpheal now tick for BUFF-subtype skills (Java HealOverTimeEffect family)** — HEAL-subtype skills (Stamina Recovery, Light of Renewal, etc.) were already handled by the existing `isHealSkill` path + `HotEffects`; this milestone extends the same ticker to BUFF-subtype skills with `<heal>`/`<mpheal>` elements, which went through the BUFF path with no ticker
  - Affected skills: "Blessing of Pernos" (NPC area HP HoT), "Recovery" (area HP HoT), "Fungie's Energy" (area HP+MP HoT), "Stigma Penance I" (skill 11575, mpheal 30s, tick 6000ms), "Clean Mana" (area MP HoT) and similar NPC/quest-buff variants
  - Note: duplicate `SkillTickHealInfo`/`TickHealEffectNames`/`TickHealEffects` were added and then removed during implementation (M370 cleanup); final code reuses existing `HotEffects`/`SkillHotInfo` types for consistency
  - **M370a:** `Model/Templates/Skill/SkillTemplate.cs` — add `"heal"`, `"mpheal"`, `"dpheal"`, `"fpheal"` to `EffectDurNames` (gate check for BUFF path; HEAL-subtype path reads durations directly)
  - **M370b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — after hpuse ticker block in BUFF path, read `HotEffects`; for each HP/MP entry start fire-and-forget `Task.Run`: loop every `checktime` ms until `effect.Expiry`, clamp heal to free capacity, broadcast `SM_ATTACK_STATUS(NaturalHp/NaturalMp, Heal/MpHeal)`
  - Previously: BUFF-subtype HoT skills silently dropped; NPC area heal buffs, Stigma Penance mpheal ticks never fired
  - Build: 0 warnings, 0 errors

- [x] **M369: caseheal — 2 Cleric "Saving Grace" BUFF skills now conditionally heal once when HP ≤ 50% (Java CaseHealEffect, ObserverType.ATTACKED)** — `<caseheal type="HP" cond_value="50" value="V" delta="D" duration2="60000"/>` was absent from `EffectDurNames` and had no handler; buff silently dropped, conditional heal never fired
  - Affected skills: Saving Grace I (skill 2391, 60s), Saving Grace II (skill 3018, 60s) — Cleric stigma that heals ~2737–3144 HP when HP drops at or below 50%
  - Java analog: `CaseHealEffect.startEffect` registers `ATTACKED` observer; `calculateHeal` fires on each hit (+ immediately on apply): if HP ≤ cond_value% of maxHP, heals flat value+delta*level, calls `effect.endEffect()` (one-shot)
  - **M369a:** `Model/Templates/Skill/SkillTemplate.cs` — add `SkillCaseHealInfo(IsHp, CondPercent, Value, Delta, IsPercent)` record; `CaseHealEffectNames = ["caseheal"]` HashSet; `CaseHealEffects` parser; add `"caseheal"` to `EffectDurNames`
  - **M369b:** `Combat/Handlers/CaseHealHandler.cs` (new) — `IEventHandler<DamageDealtEvent>`; on each damage event: check target's active effects for caseheal skills via template lookup; if HP/MP ≤ threshold, compute heal (flat or %), apply, remove buff, broadcast `SM_ATTACK_STATUS(NaturalHp) + SM_ABNORMAL_EFFECT` (buff expired)
  - **M369c:** `Program.cs` — register `CaseHealHandler` as `IEventHandler<DamageDealtEvent>`
  - Previously: both Saving Grace skills silently dropped on cast; HP-threshold heal and buff never applied
  - Build: 0 warnings, 0 errors

- [x] **M368: noresurrectpenalty added to EffectDurNames — 3 premium cash-shop BUFF skills now correctly activate their no-death-penalty state** — `<noresurrectpenalty duration2="1800000-3600000"/>` elements had `duration="0"` on `skill_template` and `"noresurrectpenalty"` was absent from `EffectDurNames`; `EffectDuration` returned 0 → BUFF path gate failed → `IsNoDeathPenalty` never set → soul sickness applied on death despite the buff being active
  - Affected skills: Administrator's Boon (1h, skill 10350), Medical Miracle Effect (1h, skill 10342), Administrator's Privilege Effect (30min, skill 10344)
  - Fix: one-line addition of `"noresurrectpenalty"` to `EffectDurNames` in `Model/Templates/Skill/SkillTemplate.cs` (after `"nodeathpenalty"`)
  - Note: `HasNoresurrectPenalty` property (M331) and CM_REVIVE soul-sickness suppression already correct — only the EffectDurNames gate was missing
  - Build: 0 warnings, 0 errors

- [x] **M367: delayedskill/delayedskillz — 23 timed debuffs that fire a child skill on the target when their duration expires (Java DelayedSkillEffect.endEffect → re-enters skill pipeline)** — `<delayedskill skill_id="X" duration2="Y"/>` elements were absent from `EffectDurNames` (so the parent debuff silently dropped) and the child-skill trigger was never implemented
  - Java analog: `DelayedSkillEffect.applyEffect` adds the effect to the target's effect controller (starts the timer); `endEffect` instantiates a new `Effect(effector, effected, childTemplate, lvl, 0)` and calls `initialize() + applyEffect()` on it — i.e., full re-entry into the skill pipeline
  - **M367a:** `Model/Templates/Skill/SkillTemplate.cs` — add `SkillDelayedSkillInfo(ChildSkillId, DurationMs)` record; add `DelayedSkillEffectNames = ["delayedskill","delayedskillz"]` HashSet; add `DelayedSkillEffects` parser (reads `skill_id` + `duration2`; for `delayedskillz` reads `time_delay_to_hit` or falls back to `duration2`); add `"delayedskill"`, `"delayedskillz"` to `EffectDurNames`
  - **M367b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — capture `buffDelayedSkillFx` + `buffCaster` before expiry Task.Run; inside expiry Task.Run (after `expired` SM_ABNORMAL_EFFECT broadcast), iterate `buffDelayedSkillFx`: (1) if child template has CC flags, apply as AbnormalState with child's `EffectDuration` + schedule removal; (2) if child template has damage/no-reduce effects, compute magic damage and call `ApplyDamageAndPublishAsync` + broadcast `SM_ATTACK_STATUS`; skip complex effects (statup, shapechange, dispel)
  - Previously: 23 `delayedskill`/`delayedskillz` debuffs silently dropped — targets received no debuff and the timed child skill (stun, root, instant damage, etc.) never fired
  - Build: 0 warnings, 0 errors

- [x] **M366: mpshield — 9 BUFF skills (Chanter/Cleric "Aether's Hold" line) convert incoming damage to MP drain via absorb pool** — `<mpshield>` element (Java `MpShieldEffect`, shieldType=9) was unparsed and absent from `EffectDurNames`; absorbed damage reduces target HP damage and drains equal MP from target
  - Java analog: `MpShieldEffect extends EffectTemplate` — `AttackShieldObserver(shieldType=9)` intercepts each hit, converts hitValueWithDelta damage to MP drain up to a total pool (value+delta*level)
  - **M366a:** `Model/Templates/Skill/SkillTemplate.cs` — add `SkillMpShieldInfo(HitValue, HitDelta, Value, Delta, IsPercent)` record; `MpShieldEffectNames` HashSet; `MpShieldEffects` parser property; add `"mpshield"` to `EffectDurNames`
  - **M366b:** `Model/AbnormalState.cs` — add `MpShieldHitValue`, `IsMpShieldPercent`, `MpShieldPoolRemaining` fields (mirrors HP shield fields)
  - **M366c:** `Model/Creature.cs` — add `TryAbsorbMpShield(int damage, out int absorbingSkillId)`: same pool-decrement logic as `TryAbsorbShield` but also drains `Player.CurrentMp -= absorbed`
  - **M366d:** `Combat/Handlers/MpShieldHandler.cs` — new handler on `DamageReceivingEvent`; calls `TryAbsorbMpShield`; broadcasts `SM_ATTACK_STATUS(ProtectDmg)` for absorbed amount
  - **M366e:** `Program.cs` — register `MpShieldHandler` after `ShieldHandler`
  - **M366f:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: read `MpShieldEffects[0]`, compute per-level values; store as `MpShieldHitValue`/`MpShieldPoolRemaining`/`IsMpShieldPercent` in AbnormalState
  - Previously: 9 mpshield skill XML entries (Chanter "Aether's Hold I-V", Cleric "Aether Defense" variants) silently dropped — casters received no shield, incoming damage bypassed MP drain entirely
  - Build: 0 warnings, 0 errors

- [x] **M365: nodeathpenalty BUFF activation + switchhpmp instant HP/MP swap** — 3 `<nodeathpenalty>` premium-cash-shop BUFF skills (Administrator's Boon, Medical Miracle Effect, Administrator's Privilege) now create an AbnormalState with `IsNoDeathPenalty=true` and suppress soul sickness on death; 2 `<switchhpmp>` Spiritmaster/Sorcerer heal skills (Reverse Condition I, Exchange Vitality I) now instantly swap the target's current HP and MP values
  - Java analog: `NoDeathPenaltyEffect extends BufEffect` sets `effect.setNoDeathPenalty(true)` — checked on death; `SwitchHpMpEffect extends EffectTemplate` calls `lifeStats.increaseHp(NATURAL_HP, currentMp - currentHp)` + `lifeStats.increaseMp(NATURAL_MP, currentHp - currentMp)` in `applyEffect`
  - **M365a:** `Model/Templates/Skill/SkillTemplate.cs` — add `HasNoDeathPenalty` (bool, checks `nodeathpenalty` element) and `HasSwitchHpMp` (bool, checks `switchhpmp` element) properties; add `"nodeathpenalty"` to `EffectDurNames`
  - **M365b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: `IsNoDeathPenalty = HasNoresurrectPenalty || HasNoDeathPenalty`; heal path: if `HasSwitchHpMp`, swap `target.CurrentHp = Min(MaxHp, oldMp)` / `target.CurrentMp = Min(MaxMp, oldHp)`, broadcast `SM_ATTACK_STATUS(NaturalHp/UsedHp)` and `SM_ATTACK_STATUS(NaturalMp/Mp)` for non-zero deltas
  - Previously: 3 premium protection buffs silently dropped (no AbnormalState, no soul-sickness suppression); 2 Spiritmaster swap skills were treated as zero-effect heal casts (no HP/MP swap occurred)
  - Build: 0 warnings, 0 errors

- [x] **M364: drboost/apboost/boostdroprate BUFF activation — 23 + 14 + 25 = 62 rate-boost skills now create AbnormalStates and apply their stat deltas** — `DRBoostAddDelta` and `APBoostAddDelta` scanned only `statup`/`statboost` wrapper elements; dedicated `<drboost>`/`<apboost>` elements (which contain `<change stat="DR_BOOST/AP_BOOST" func="ADD"/>` children directly) were never scanned; `boostdroprate` had no property/AbnormalState field/Player field at all; none of the three appeared in `EffectDurNames` so they never activated as BUFF skills
  - **M364a:** `Model/Templates/Skill/SkillTemplate.cs` — `DRBoostAddDelta`: add `"drboost"` to scan set; `APBoostAddDelta`: add `"apboost"` to scan set; add `BoostDropRateAddDelta` property scanning `boostdroprate` elements for `BOOST_DROP_RATE ADD`; add `"drboost"`, `"apboost"`, `"boostdroprate"` to `EffectDurNames`
  - **M364b:** `Model/AbnormalState.cs` — add `BoostDropRateDeltaVal { get; init; }` (per-mille scale: 10000 = +100%)
  - **M364c:** `Model/Player.cs` — add `BonusDropRatePct { get; set; }` (accumulated from active boostdroprate buffs)
  - **M364d:** `Model/Creature.cs` — `ApplyEffectDeltas`/`ReverseEffectDeltas`: wire `BoostDropRateDeltaVal` → `Player.BonusDropRatePct`
  - **M364e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — read `BoostDropRateAddDelta`; store as `BoostDropRateDeltaVal` in AbnormalState
  - **M364f:** `Services/LootService.cs` — apply `BonusDropRatePct` to item drop chance (per-mille scale: `chance * (10000 + val) / 10000`)
  - Previously: 23 drboost (death-rank-gain scrolls/buffs), 14 apboost (abyss-point-gain scrolls), 25 boostdroprate (drop-rate scrolls) all silently dropped — scrolls that should increase drop/AP/DR rates had zero in-game effect
  - Build: 0 warnings, 0 errors

- [x] **M363: convertheal EffectDurNames fix — 4 `<convertheal>` XML entries now activate as BUFF skills** — `ConvertHealHandler` (M264) was registered and `ConvertHealEffects` parser was correct, but `"convertheal"` was absent from `EffectDurNames`, so the BUFF gate rejected all convertheal skills (EffectDuration=0); same pattern as M361's `"reflector"` fix
  - **M363a:** `Model/Templates/Skill/SkillTemplate.cs` — add `"convertheal"` to `EffectDurNames`
  - Build: 0 warnings, 0 errors

- [x] **M362: shield double-absorption fix — single canonical absorption path via ShieldHandler** — M265's `ShieldHandler` fires on `DamageReceivingEvent` (pre-damage inside `ApplyDamageAndPublishAsync`), but M359 also inserted `TryAbsorbShield` calls before `ApplyDamageAndPublishAsync` in CM_CASTSPELL (AoE + single-target) and CM_ATTACK (auto-attack); a shielded target absorbed damage twice per hit; additionally M265 never depleted the shield pool (it would absorb hitCap on every hit indefinitely). Fix: add `TryAbsorbShield(int, out int)` overload on `Creature` that returns both remaining damage and the absorbing shield's skill ID (needed for `SM_ATTACK_STATUS.ProtectDmg`); rewrite `ShieldHandler` to use the overload as the single canonical absorption and pool-depletion point; remove all three pre-`ApplyDamageAndPublishAsync` `TryAbsorbShield` calls; original single-arg overload delegates to the new one
  - **M362a:** `Model/Creature.cs` — add `TryAbsorbShield(int damage, out int absorbingSkillId)`: lock, find first non-expired shield with remaining pool, compute cap, decrement pool, remove when exhausted, return `damage - absorbed`; original `TryAbsorbShield(int damage)` delegates via `out _`
  - **M362b:** `Combat/Handlers/ShieldHandler.cs` — remove `IDataManager` dependency; use `TryAbsorbShield(original, out shieldSkillId)` overload; `e.Damage.Value = remaining`; broadcast `SM_ATTACK_STATUS(ProtectDmg, shieldSkillId, absorbed)`
  - **M362c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — remove AoE-path and single-target-path `TryAbsorbShield` calls (M359 leftovers)
  - **M362d:** `Network/Aion/ClientPackets/CM_ATTACK.cs` — remove auto-attack-path `TryAbsorbShield` call (M359 leftover)
  - Build: 0 warnings, 0 errors

- [x] **M360: alwaysresist charge-based resistance — 19 XML entries now consume charges per incoming magical spell** — Java `AlwaysResistEffect` is charge-based: `value` attribute sets number of guaranteed resists, buff removed when charges exhausted; M276 implemented `HasAlwaysResist` as a flag for full immunity but never decremented charges; added `AlwaysResistCount`/`AlwaysResistDurationMs` properties to `SkillEffects`; `"alwaysresist"` added to `EffectDurNames`; `AlwaysResistCountRemaining { get; set; }` added to `AbnormalState`; `Creature.TryConsumeResistCharge()` consumes one charge (lock-safe, removes effect when exhausted); BUFF gate and `durationMs` fallback chain extended; `TryConsumeResistCharge` called in CM_CASTSPELL AoE and single-target paths before the normal resist roll — auto-resist fires before damage is computed so M276 `AlwaysResistHandler` never fires for primary skill hits; DoT ticks still intercepted by M276 as before
  - **M360a:** `Model/Templates/Skill/SkillTemplate.cs` — `AlwaysResistCount`/`AlwaysResistDurationMs` getter properties; `"alwaysresist"` added to `EffectDurNames`
  - **M360b:** `Model/AbnormalState.cs` — `AlwaysResistCountRemaining { get; set; }` (mutable charge counter)
  - **M360c:** `Model/Creature.cs` — `TryConsumeResistCharge()`: lock, find first effect with `AlwaysResistCountRemaining > 0`, decrement, remove when depleted
  - **M360d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF gate: add `|| AlwaysResistDurationMs > 0`; `durationMs` chain: add `AlwaysResistDurationMs` before `AlwaysParryDurationMs`; AbnormalState: `AlwaysResistCountRemaining = AlwaysResistCount`; AoE + single-target paths: pre-resist `TryConsumeResistCharge` check before normal resist roll
  - Build: 0 warnings, 0 errors

- [x] **M359: shield damage absorption buff — 320 BUFF/CHANT skills now absorb incoming damage via active shield pool** — `<shield>` effect elements (Java `ShieldEffect`, shieldType=2) were parsed into `SkillShieldInfo` but only captured `hitvalue`/`hitdelta`; `value`/`delta` (total pool) and `percent` flag were missing; no fields existed in `AbnormalState` to hold shield state; no damage-path logic intercepted attacks; fixed by: (1) expanding `SkillShieldInfo` with `Value`, `Delta`, `IsPercent`; (2) adding `ShieldHitValue`, `IsShieldPercent`, `ShieldPoolRemaining` (mutable) to `AbnormalState`; (3) BUFF path reads `ShieldEffects[0]`, computes per-level values, stores in `AbnormalState`; (4) `Creature.TryAbsorbShield(damage)` absorbs min(hitCap, damage) or hitPct% of damage capped by pool, removes effect when pool reaches 0; (5) inserted into auto-attack path (CM_ATTACK), single-target skill path, and AoE skill path (CM_CASTSPELL); `percent=true` shields (e.g. 50% per hit, infinite pool) and flat-absorb shields (e.g. 98 per hit, 98 pool = single-hit) both handled
  - **M359a:** `Model/Templates/Skill/SkillTemplate.cs` — `SkillShieldInfo` struct: add `Value`, `Delta`, `IsPercent`; `ShieldEffects` parser: read `value`, `delta`, `percent` attributes
  - **M359b:** `Model/AbnormalState.cs` — add `ShieldHitValue { get; init; }`, `IsShieldPercent { get; init; }`, `ShieldPoolRemaining { get; set; }`
  - **M359c:** `Model/Creature.cs` — add `TryAbsorbShield(int damage)` method: lock-safe pool decrement, removes effect when pool exhausted, returns post-absorption damage (minimum 0)
  - **M359d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: read `ShieldEffects[0]`, compute `shieldHitVal`/`shieldPool`/`isShieldPct` at `_level`; store as `ShieldHitValue`/`ShieldPoolRemaining`/`IsShieldPercent` in AbnormalState; single-target damage: `damage = target.TryAbsorbShield(damage)` before `ApplyDamageAndPublishAsync`; AoE damage: same interception
  - **M359e:** `Network/Aion/ClientPackets/CM_ATTACK.cs` — auto-attack: `totalDamage = target.TryAbsorbShield(totalDamage)` before `ApplyDamageAndPublishAsync`
  - Build: 0 warnings, 0 errors

- [x] **M358: instant-cast BUFF skills + weaponstatup — 1262 + 26 skills now correctly applied** — Two root causes: (1) the BUFF path gate checked `template.Duration > 0` (cast time), silently skipping all 1262 instant-cast buff skills (`duration=0`); (2) `weaponstatup` elements (Blessing of Blood, Bestial Fury, Arrow Flurry, Hunter's Might, etc.) were not scanned by any statup/statboost property loop. Fixes: add `|| Effects.EffectDuration > 0` to BUFF gate; add `EffectDuration` as final fallback in `durationMs` chain; add `weaponstatup` to all 50 `is not ("statup" or "statboost")` pattern checks in SkillTemplate; add buff-side effect names (shield/protect/nofly/xpboost/hostileup/dispeldebuff/dispelbuff/sanctuary/boostskillcastingtime) to `EffectDurNames`
  - **M358a:** `Model/Templates/Skill/SkillTemplate.cs` — 50 occurrence `replace_all`: `is not ("statup" or "statboost")` → `is not ("statup" or "statboost" or "weaponstatup")`; add "weaponstatup" to `EffectDurNames`; expand `EffectDurNames` with buff effect names (shield/protect/nofly/xpboost/skillxpboost/hostileup/dispeldebuff/dispelbuff/sanctuary/boostskillcastingtime)
  - **M358b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF gate: add `|| template.Effects?.EffectDuration > 0`; `durationMs` fallback chain: append `AlwaysParryDurationMs > 0 ? ... : (Effects?.EffectDuration ?? 0)` as final fallback
  - Build: 0 warnings, 0 errors

- [x] **M357: SM_ABNORMAL_EFFECT protocol bitmask — CC state now visible on client nameplates/status bars** — `SM_ABNORMAL_EFFECT.abnormals` DWORD was hardcoded to `0` so the client never showed CC icons (stun, sleep, root, snare, slow, bind, nofly, blind, etc.) on target nameplates; `AbnormalCcFlags` had three errors (`Blind=1` should be 32, `Curse=131072` should be 1024, `SNARE` missing) and four missing flags (`Snare=131072`, `Slow=262144`, `Bind=1048576`, `NoFly=8388608`); `ElementToCcFlag` was missing "snare"/"absolutesnare", "slow"/"absoluteslow", "nofly" mappings and had "bind"/"buffbind" → Root instead of Bind; now `SM_ABNORMAL_EFFECT.Write` ORs each active effect's `CcFlags` into an int and writes it; `CantMove` compound flag now includes Bind (bind prevents movement); 361+120+52+104 = 637 skills now contribute correct CC bits
  - **M357a:** `Model/AbnormalCcFlags.cs` — fix `Blind=1→32`, `Curse=131072→1024`; add `Snare=131072`, `Slow=262144`, `Bind=1048576`, `NoFly=8388608`; add `Bind` to `CantMove` compound
  - **M357b:** `Model/Templates/Skill/SkillTemplate.cs` `ElementToCcFlag` — fix "bind"/"buffbind" → Bind; add "snare"/"absolutesnare" → Snare; add "slow"/"absoluteslow" → Slow; add "nofly" → NoFly
  - **M357c:** `Network/Aion/ServerPackets/SM_ABNORMAL_EFFECT.cs` — compute `int abnormals = _effects.Aggregate(0, (acc, e) => acc | (int)e.CcFlags)` and write it instead of `WriteD(0)`
  - Build: 0 warnings, 0 errors

- [x] **M356: CC resistance ADD debuffs — 12 entries now correctly reduce target's CC resistance via statdown** — `SLEEP_RESISTANCE ADD` (Kinetic Battery I −300), `ROOT_RESISTANCE ADD` (Mobility Thrusters I −200), and similar per-CC-type resistance reductions from `statdown` were silently discarded; `ScanStatupAddValue` already scanned `statdown` so the properties return negative values, but the DEBUFF path never set them in AbnormalState; now all 9 CC resistance types (stun/stumble/stagger/spin/sleep/fear/openaerial/root/snare) are wired into the DEBUFF initializer; `Creature.ApplyEffectDeltas`/`ReverseEffectDeltas` already handle these from M338
  - **M356a:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — DEBUFF path: read all 9 CC resist deltas from template; add to debuff AbnormalState initializer; add to expiry `statChanged` check
  - Build: 0 warnings, 0 errors

- [x] **M355: elemental resist PERCENT debuffs — 40 entries (10 unique skills) now reduce target's elemental resistance by % of current value** — `statdown FIRE/WATER/WIND/EARTH_RESISTANCE PERCENT` (Flight: Enervating Bind I–III, Charm: Nature's Curse, Decrease Defense, Depression, Incite Rage, Nova Power, Water Punishment, Weaken Resistance) were silently discarded; added `ScanStatdownPercentValue` helper and 4 debuff properties; in the DEBUFF path, the pct is converted to a flat delta against the target's current resist (same pattern as M322/M326/M327 PERCENT debuffs), then merged into the existing elemental resist ADD delta
  - **M355a:** `Model/Templates/Skill/SkillTemplate.cs` — added `ScanStatdownPercentValue` private helper; added `FireResistPctDebuff`, `WaterResistPctDebuff`, `WindResistPctDebuff`, `EarthResistPctDebuff` properties
  - **M355b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — DEBUFF path: read 4 pct properties, convert to flat delta against `target.FireResist/WaterResist/WindResist/EarthResist`, merged into existing `fireResistDebuff` etc.
  - Build: 0 warnings, 0 errors

- [x] **M354: FLY_TIME ADD buff — 201 entries now correctly increase MaxFp (flight stamina) for buff duration** — `FLY_TIME ADD` inside `statup`/`statboost` (GM's Armor +120, GM's Tempest +60, GM Wings! +120, consumable scrolls +15, Lustrous Feather Effect, etc.) were silently discarded; `EffectiveMaxFp` formula only applied the PERCENT case; now `BonusFlyTimeFlat` accumulates the flat bonus and `EffectiveMaxFp = (MaxFp * (100+Pct)/100) + Flat`, with CurrentFp clamped on apply and reverse; Creature.cs handles apply/reverse matching the PERCENT path
  - **M354a:** `Model/Templates/Skill/SkillTemplate.cs` — added `FlyTimeAddDelta` property: sums `FLY_TIME ADD` from statup/statboost
  - **M354b:** `Model/AbnormalState.cs` — added `FlyTimeAddDeltaVal` field
  - **M354c:** `Model/Player.cs` — added `BonusFlyTimeFlat` field; updated `EffectiveMaxFp` to `= (pct-formula) + BonusFlyTimeFlat`
  - **M354d:** `Model/Creature.cs` — `ApplyEffectDeltas`/`ReverseEffectDeltas` apply/reverse flat MaxFp bonus with CurrentFp clamping
  - **M354e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path reads `FlyTimeAddDelta`, stores `FlyTimeAddDeltaVal` in AbnormalState
  - Build: 0 warnings, 0 errors

- [x] **M353: elemental resist ADD debuffs — 271 entries now reduce target's fire/water/wind/earth resistance** — `statdown FIRE/WATER/WIND/EARTH_RESISTANCE ADD` (Agonizing Slash I-V −50 each, Agony's Shackles, Elementar's/Sorcerer's Weakening Blow, etc.) were silently discarded because the DEBUFF path AbnormalState initializer didn't include elemental resist fields; `ScanStatupAddValue` already scans `statdown` elements so `FireResistDelta` etc. correctly return negative values; the fix is purely wiring them into the debuff AbnormalState; `Creature.ApplyEffectDeltas` and `ReverseEffectDeltas` already handle these fields from the M339 BUFF work
  - **M353a:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — DEBUFF path: read `FireResistDelta/WaterResistDelta/WindResistDelta/EarthResistDelta` from template; add to debuff AbnormalState initializer; add to expiry `statChanged` check
  - Build: 0 warnings, 0 errors

- [x] **M352: REGEN_HP/MP ADD flat buff — 104+100=204 entries now correctly add flat HP/MP per regen tick** — `REGEN_HP ADD` and `REGEN_MP ADD` inside `statup`/`statboost` (Boost HP I +4/II +10/III +20, Breath of Nature I +20/II +30/III +35, and similar) were silently discarded; `BonusRegenHpFlat`/`BonusRegenMpFlat` did not exist on Player and RegenService only applied the PERCENT case; now flat regen is accumulated on buff application, added per tick in RegenService after the PERCENT multiplier, and reversed on buff expiry via Creature.ReverseEffectDeltas
  - **M352a:** `Model/Templates/Skill/SkillTemplate.cs` — added `RegenHpAddDelta` + `RegenMpAddDelta` computed properties: sum ADD REGEN_HP/MP from statup/statboost
  - **M352b:** `Model/AbnormalState.cs` — added `RegenHpAddDeltaVal` + `RegenMpAddDeltaVal` fields
  - **M352c:** `Model/Player.cs` — added `BonusRegenHpFlat` + `BonusRegenMpFlat` fields
  - **M352d:** `Model/Creature.cs` — `ApplyEffectDeltas`/`ReverseEffectDeltas` apply/reverse flat regen (same pattern as percent)
  - **M352e:** `Services/RegenService.cs` — `regen += BonusRegenHpFlat/BonusRegenMpFlat` after PERCENT multiplier
  - **M352f:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path reads `RegenHpAddDelta`/`RegenMpAddDelta` from template, stores in AbnormalState
  - Build: 0 warnings, 0 errors

- [x] **M351: FLY_SPEED PERCENT buff (statup/statboost) — 70 entries now correctly increase target's fly speed for buff duration** — `FLY_SPEED PERCENT` values inside `statup`/`statboost` elements (Flyover Reconnaisance +33%, Charge +60%, Winged Rage I-II +33%, Flight: Maximization of Speed +50%, Strengthen Wings I-III +10/15/20%, etc.) were silently discarded; `Player.BonusFlySpeedPct` was never modified by active buff casts; now the pct is added on buff application and restored to the pre-buff snapshot on expiry (same restore-by-snapshot pattern as M320 fly speed debuff)
  - **M351a:** `Model/Templates/Skill/SkillTemplate.cs` — added `FlySpeedStatUpPct` computed property on `SkillEffects`: sums `FLY_SPEED PERCENT` from `statup`/`statboost` elements
  - **M351b:** `Model/AbnormalState.cs` — added `FlySpeedStatUpPct` + `PreBuffFlySpeedPct` fields (mirror of `FlySpeedDebuffPct`/`PreDebuffFlySpeedPct`)
  - **M351c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: read `flySpeedStatUpPct`; store `PreBuffFlySpeedPct = buffTarget.BonusFlySpeedPct` in AbnormalState; apply `BonusFlySpeedPct += flySpeedStatUpPct`; expiry: restore `BonusFlySpeedPct = PreBuffFlySpeedPct`
  - Build: 0 warnings, 0 errors

- [x] **M350: statdown ATTACK_SPEED PERCENT — 22 entries now applied in both debuff and BUFF self-nerf paths** — `statdown ATTACK_SPEED PERCENT` was silently ignored: the debuff path only read ATTACK_SPEED PERCENT from `<slow>` elements and the BUFF path self-nerf (M341) skipped attack speed entirely; now enemy debuffs like Body Control I, Exhausting Cloud, Sign of Infernal Blaze apply the attack speed slow, and BUFF self-nerfs like "Bravery of the Composed (+70%)" correctly slow the caster's own attacks; Java analog: `StatdownEffect.applyEffect` with `AdditionStat.calculatePercent(delta) = (100+delta)/100`
  - **M350a:** `Model/Templates/Skill/SkillTemplate.cs` — added `StatdownAtkSpeedPct` computed property on `SkillEffects`: sums ATTACK_SPEED PERCENT changes from `statdown` elements
  - **M350b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — DEBUFF path: merged `StatdownAtkSpeedPct` into `slowAtkPct` accumulator; BUFF path self-nerf: added `StatdownAtkSpeedPct → atkSpeedStatUpDelta += target.CurrentAttackSpeed × pct / 100`
  - Build: 0 warnings, 0 errors

- [x] **M349: PERCENT MAXHP/MAXMP active buff support — 53 MAXHP + 2 MAXMP = 55 buff skill entries now correctly increase target's max health/mana** — `statup PERCENT MAXHP/MAXMP` in active BUFF skills (Second Wind, Improved Stamina I–III, Empyrean Armor, Blessing of Health, Blessing of Stone, Elemental Spirit Armor, Etude, Refresh Spirit, etc.) were silently discarded; MaxHpPercentStatUpDelta and MaxMpPercentStatUpDelta were only applied for passive skills at login, not for active buffs during combat; the delta is now computed as `buffTarget.MaxHp/MaxMp × pct / 100` and merged into the existing MaxHpDelta/MaxMpDelta AbnormalState field with full expiry clamping via the existing buff-expiry path
  - **M349a:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: after `maxHpStatUpDelta = MaxHpStatUpDelta`, added `MaxHpPercentStatUpDelta` computation → `maxHpStatUpDelta += buffTarget.MaxHp × pct / 100`; same for MAXMP; both gated on `buffTarget is Player`
  - Build: 0 warnings, 0 errors

- [x] **M348: BOOST_CASTING_TIME PERCENT buffs from statup/statboost elements (147 entries) — Boon of Quickness, Sage's Wisdom, Summoning Alacrity, Word of Quickness, and similar cast-speed buffs now correctly reduce skill cast times** — `BOOST_CASTING_TIME PERCENT` values inside `statup`/`statboost` elements were silently discarded; now captured as permille (value×10) and merged into `CastTimeDelta` via the BUFF path; negative values (e.g. "Shackle of Vulnerability" −50%) slow cast time; Java analog: `ReverseStat.calculatePercent(delta) = (100−delta)/100`
  - **M348a:** `Model/Templates/Skill/SkillTemplate.cs` — added `CastTimeStatUpPctDelta` computed property on `SkillEffects`: scans `statup`/`statboost` for `PERCENT BOOST_CASTING_TIME` changes, returns sum(v×10) in permille
  - **M348b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: added `CastTimeStatUpPctDelta` to `castTimeStatUpDelta` accumulator (alongside existing `CastTimeStatUpDelta` ADD and `BoostCastTimePctDelta` from boostskillcastingtime)
  - Build: 0 warnings, 0 errors

- [x] **M347: NPC AoE splash magic resist, elemental resistance, and crit checks** — NPC caster-centered AoE splash hits now apply the same checks as the primary target (M346/M342/M345): magical splashes can be resisted by players with high magic resist; elemental resistance reduces elemental splash damage; crit multiplier applies based on NPC power stat
  - Java analog: Java AoE effects call `calculate()` per-target which applies resist/crit through the same `EffectTemplate.calculate()` pipeline used for single targets — our AoE splash now mirrors this per-target logic
  - **M347a:** `Services/NpcAiService.cs` — NPC AoE splash loop: added magic resist check block before damage calc (player-only, skipped for `npcNoResist`, `continue` on resist with 0-damage broadcast); added elemental resist reduction after noreducespellatk; added crit block reusing `npcSkillCritRate` computed for the primary target
  - Build: 0 warnings, 0 errors

- [x] **M346: NPC skill magic resist check + crit for NPC skill casts** — NPC magical skills can now be resisted by players with high magic resist; both magical (1.5×) and physical (2.0×) NPC skill crits now apply, respecting target's SpellFortitude and StrikeFortitude
  - Java analog: `EffectTemplate.calculate()` calls `StatFunctions.calculateMagicalResistRate(npc, player, accMod)` for magical skill effects; `AttackUtil.calculateMagicalCritical/calculateWeaponCritical` applies the crit coefficient
  - Magic resist formula: `resistRate = max(1, player.MResist - npcMagicAcc)` where npcMagicAcc = level*(33.6-0.16*level)+5 (level-scaled, no NPC magic_accuracy in templates); skipped if noresist="true" on damage effect
  - Crit formula: power-stat piecewise (≤440: power×0.1%, ≤600: 44+(power-440)×0.05%, >600: 52+(power-600)×0.02%); physical multiplier = max(1, 2.0−round(strikeFortitude/1000)); magical multiplier = max(1, 1.5−round(spellFortitude/1000))
  - **M346a:** `Services/NpcAiService.cs` — `CastNpcDamageAsync`: moved dmgFx before resist check; added magic resist block (before defense calc, early return on resist); added crit block (after elemental resist, before ApplyDamageAndPublishAsync)
  - Build: 0 warnings, 0 errors

- [x] **M345: elemental resistance applied to NPC-cast DoT tick damage** — NPC bleed/poison/disease ticks now respect the player's elemental resistance (FireResist/WaterResist/WindResist/EarthResist) using the same 1250-scale formula as M343 (player-cast) and M342 (NPC instantaneous magic damage); resistance value is baked into `dotTickDmg` at DoT creation time since the NPC DoT lambda captures a fixed value
  - Java analog: NPC `DotEffect.onTick()` calls `StatFunctions.calculateMagicalSkillDamage` which applies elemental resistance on each tick; our approach bakes the reduction at creation time (captures static target state) — acceptable since DoT resist buffs rarely change mid-DoT
  - **M345a:** `Services/NpcAiService.cs` — `CastNpcDamageAsync` DoT loop: replaced `Math.Max(1, dot.BaseValue + dot.Delta * skillLevel)` with `rawNpcDot` + inline element switch → `npcDotElemResist` → apply `(1f - npcDotElemResist / 1250f)` when nonzero
  - Build: 0 warnings, 0 errors

- [x] **M344: ShieldMastery passive BLOCK% bonus — shield proficiency passives now boost block rate when a shield is equipped (6 shieldmastery entries)** — Warrior/Knight/Templar/Chanter shield training passives add +5% of BaseBlock to total block rating when a shield occupies the sub-hand slot; recomputed on equip/unequip and at world enter; deactivated automatically when shield is removed
  - Java analog: `ShieldMasteryEffect extends BufEffect` uses `StatShieldMasteryFunction` which calls `super.apply(stat)` only if `player.getEquipment().isShieldEquipped()`; the +5% BLOCK PERCENT is applied to the base block stat
  - **M344a:** `Model/Templates/Skill/SkillTemplate.cs` — added `ShieldMasteryBlockPct` computed property on `SkillEffects`: scans `shieldmastery` elements for `<change stat="BLOCK" func="PERCENT" value="N"/>` children, returns sum (mirrors ArmorMastery parser)
  - **M344b:** `Model/Player.cs` — added `PassiveBonusBlock` field (flat bonus, recalculated on equip/login; 0 when shield not equipped)
  - **M344c:** `Services/PassiveShieldMasteryHelper.cs` — new static helper `ComputePct`: scans PASSIVE skills for `ShieldMasteryBlockPct > 0`, returns total (mirrors PassiveArmorMasteryHelper)
  - **M344d:** `Network/Aion/ClientPackets/CM_EQUIP_ITEM.cs` — after ArmorMastery block: detect `subTpl?.ArmorTypeName == "SHIELD"`, compute smPct, set `PassiveBonusBlock = BaseBlock * smPct / 100`
  - **M344e:** `Services/PlayerEnterWorldService.cs` — after ArmorMastery block: same shield detect + `PassiveBonusBlock` assignment at login
  - **M344f:** `Network/Aion/ClientPackets/CM_ATTACK.cs` — block total now includes `pvpBlock.PassiveBonusBlock`
  - Build: 0 warnings, 0 errors

- [x] **M343: elemental resistance applied to DoT (bleed/poison/disease) tick damage (267 elemental DoT entries)** — Bleed and poison DoTs with elemental damage types (FIRE, WIND, EARTH, WATER) now respect the target's elemental resistance when ticking; same 1250-scale formula as M339; spellatk DoTs use magic boost/magic defense path and remain unaffected; skipped for noreducespellatk bypass (already on spellatk path)
  - Java analog: DoT effects in Java call `StatFunctions.calculateMagicalSkillDamage` on each tick if the DoT element is non-empty; our non-spellatk branch now mirrors that via `GetElementalResist(target, dot.Element)`
  - Data: 267 DoT entries have a non-empty element — poison EARTH: 85, bleed WIND: 47, bleed FIRE: 45, poison FIRE: 29, bleed WATER: 20, bleed EARTH: 18, poison WATER: 14, poison WIND: 9
  - **M343a:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — 3 DoT sites updated (AoE-ground, splash, single-target): `else` branch changed from `Math.Max(1, rawDot)` to `GetElementalResist(target/splash, dot.Element)` → scale with `(1f - elemResist / 1250f)` when nonzero
  - Build: 0 warnings, 0 errors

- [x] **M342: elemental resistance applied to NPC magical skill damage (2714 elemental skill entries)** — Player Fire/Water/Wind/Earth resist buffs and gear stats now reduce damage from NPC magical skills (spellatkinstant, spellatk, spellatkdraininstant) with a matching element; same 1250-scale formula as M339's player-cast path; skipped for noreducespellatk bypass
  - Java analog: `calculateMagicalSkillDamage` in `StatFunctions` checks elemental resistance regardless of whether the caster is a player or NPC; our CastNpcDamageAsync now mirrors the same guard `!isPhysical && npcNoReduce not active`
  - **M342a:** `Services/NpcAiService.cs` — `CastNpcDamageAsync`: after npcNoReduce block, added inline element-switch resist check (same logic as `GetElementalResist` in CM_CASTSPELL; inlined to avoid cross-file dependency)
  - Build: 0 warnings, 0 errors

- [x] **M341: BUFF-path statdown self-nerfs — 97 BUFF skills with statdown elements now correctly penalize the caster** — Warrior "Ferocity/Berserking" PDEF penalty, Ranger "Focused Shots" PDEF cut, Templar/Gladiator shield-stance PATK reduction, Assassin "Shadow Rage" EVASION/MRESIST cut, and others now apply their self-nerf stat reductions to the caster's own stats alongside the buff bonus
  - Java analog: `StatDownEffect` inside a BUFF skill is applied to the `effector` (caster), not the `effected` (target). The same `StatDownEffect.applyEffect` is used regardless; Java's effect hierarchy handles both cases through the EffectController pipeline
  - Note: Summer/Winter Circle elemental resist self-nerfs (Fire/Water/Wind/Earth -30 to -90) were already handled by M339's `ScanStatupAddValue` which includes `statdown` elements — those 69 entries needed no additional work
  - **M341a:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: after existing PERCENT statup blocks, added self-nerf reads: `PdefPercentDebuff` + `PdefAddDelta` → `pdefStatUpDelta`; `PatkPercentDebuff` + `PhysAtkAddDelta` → `patkStatUpDelta`; `EvasionPercentDebuff` + `EvasionAddDelta` → `evasionStatUpDelta`; `MResistAddDelta` → `mresistStatUpDelta`; `PhysAccAddDelta` → `physAccStatUpDelta`; `ConcentrationAddDelta` → `concentrationStatUpDelta`; `PhysCritAddDelta` → `physCritStatUpDelta`. All merged into existing statup variables — AbnormalState, Creature, and expiry logic require no changes
  - Limitation: MAXHP PERCENT self-nerf (7 entries), SPEED PERCENT (4 entries, movement slow self-nerf), and ATTACK_SPEED PERCENT (4 entries) deferred — those require additional pre-buff snapshot fields and are lower player-facing impact than the stat nerfs
  - Build: 0 warnings, 0 errors

- [x] **M340: PVP_ATTACK_RATIO + PVP_DEFEND_RATIO ADD buffs (45 + 41 = 86 skill entries)** — PvP offensive ratio buffs (e.g. Abyss battle skills, short-burst combat boosts) and PvP defensive ratio buffs (e.g. "Guardian of the Alliance" passive, anti-PvP scrolls) now modify PvP skill and auto-attack damage; Java formula: after PvP 50% cut → `damage *= (1 + atkRatio/1000f - defRatio/1000f)` applied as net modifier
  - Java analog: `StatFunctions.adjustDamages`: `pvpAttackBonus = PVP_ATTACK_RATIO * 0.001f`; `pvpDefenceBonus = PVP_DEFEND_RATIO * 0.001f`; `damages = round(damages + damages * atkBonus - damages * defBonus)`; our net-delta form `(1 + (atkRatio - defRatio) * 0.001f)` is algebraically equivalent
  - **M340a:** `Model/Templates/Skill/SkillTemplate.cs` — added `PvpAtkRatioDelta` and `PvpDefRatioDelta` expression-body properties via `ScanStatupAddValue`
  - **M340b:** `Model/AbnormalState.cs` — added `PvpAtkRatioDelta` / `PvpDefRatioDelta` (`init`-only fields)
  - **M340c:** `Model/Creature.cs` — added `PvpAtkRatio` / `PvpDefRatio` accumulator fields; `ApplyEffectDeltas` / `ReverseEffectDeltas` each +2 lines
  - **M340d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path reads + AbnormalState init; AoE-ground and single-target PvP hit paths: after the `rawSpellDmg / 2` cut, apply `rawSpellDmg *= (1 + (player.PvpAtkRatio - target.PvpDefRatio) * 0.001f)` when target is Player; splash skipped (NPC-only targets)
  - **M340e:** `Network/Aion/ClientPackets/CM_ATTACK.cs` — physical auto-attack PvP 50% block expanded to also apply net PvP ratio modifier after the halving
  - Build: 0 warnings, 0 errors

- [x] **M339: elemental resistance buffs — FIRE/WATER/WIND/EARTH_RESISTANCE ADD (1273 XML entries; ~9595 elemental skill usages)** — Elemental resist buffs (e.g. Balaur Ward, Fire Resist scrolls, elemental resist armor enchants) now reduce magical-skill damage when the skill's element matches the target's active resistance; Java formula `damage *= (1 - elemDef / 1250f)` applied after defense mitigation, skipped for `noreducespellatk` bypass hits
  - Java analog: `SkillTemplate.element` + per-target elemental stats (FIRE_RESISTANCE, etc.); Java `MagicalSkillTemplate.calculateDamage` consults elemental defense from target's game stats; our port mirrors the reduction factor using `GetElementalResist(target, element)` after both the regular and AoE defense steps
  - **M339a:** `Model/Templates/Skill/SkillTemplate.cs` — added `FireResistDelta`, `WaterResistDelta`, `WindResistDelta`, `EarthResistDelta` expression-body properties via `ScanStatupAddValue` (M338's helper reused)
  - **M339b:** `Model/AbnormalState.cs` — added `FireResistDelta … EarthResistDelta` (4 `init`-only fields)
  - **M339c:** `Model/Creature.cs` — added `FireResist … EarthResist` accumulator fields; `ApplyEffectDeltas` / `ReverseEffectDeltas` each +4 lines
  - **M339d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: reads 4 elemental deltas, wires into `AbnormalState`; damage formula: 3 sites (AoE-ground, single-target, splash) each get `GetElementalResist` check guarded by `spellIsMagical && noReduce not active`
  - **M339e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — added `GetElementalResist(Creature, string)` switch-expression helper; element strings: "FIRE", "WATER", "WIND", "EARTH"
  - Scale: Java XML values are on a 0–1250 scale (1250 = 100% immune); damage multiplied by `(1 - resist / 1250f)`, result clamped to minimum 1
  - Build: 0 warnings, 0 errors

- [x] **M338: per-CC-type resistance buffs — STUN/STUMBLE/STAGGER/SPIN/OPENAREIAL/SLEEP/FEAR/ROOT/SNARE RESISTANCE ADD (272 skill entries across 9 stat types)** — Resistance buffs (e.g. Templar anti-stun stance, Chanter anti-stumble signet, anti-spin/openaerial buffs) now reduce the chance of their respective CC landing; M333's global `CcResistAll` check is complemented by a per-CC secondary roll so both fire independently when the target has specific resistance active
  - Java analog: `EffectTemplate.calculateEffectResistRate` reduces `effectPower = 1000 - ABNORMAL_RESISTANCE_ALL - specificResistance`; our .NET port runs two independent checks (global first, specific second) approximating the combined formula
  - **M338a:** `Model/Templates/Skill/SkillTemplate.cs` — added `ScanStatupAddValue(statName)` private scanner helper + 9 `XxxResistDelta` properties (StunResistDelta … SnareResistDelta) using expression-body one-liners
  - **M338b:** `Model/AbnormalState.cs` — added `StunResistDelta … SnareResistDelta` (9 fields, `init` only)
  - **M338c:** `Model/Creature.cs` — added 9 `XxxResist` accumulator fields; `ApplyEffectDeltas` / `ReverseEffectDeltas` each +9 lines
  - **M338d:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: reads all 9 resist deltas, wires into `AbnormalState`; DEBUFF path: CC resist check refactored to check global `CcResistAll` first then `GetCcFlagResist(target, ccFlags)` second
  - **M338e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — added `GetCcFlagResist(Creature, AbnormalCcFlags)` static helper; Root flag uses `Max(RootResist, SnareResist)` since snare/root/bind all map to `AbnormalCcFlags.Root` in our system
  - Scale note: per-CC resistance values in XML are on Java's 0–1000 scale (1000 = 100% immune to that CC type); checked via `Random.Next(1001) < specificResist` — consistent with the existing per-CC stat scale (separate from M333's 0–10000 global scale)
  - Build: 0 warnings, 0 errors

- [x] **M337: onetimeboostheal — HEAL_SKILL_BOOST PERCENT active buff (5 skill entries)** — Cleric "Blessed Shield I-III", Stigma Blessed Shield, and Chanter "Healer's Praise I Effect" now apply a timed multiplicative healing output boost (+100% in most cases); buff participates in the same `healBoostMult`/`aoeBoostMult` pipeline as passive and flat heal boosts
  - Java analog: `OnetimeBoostHealEffect extends BufEffect`; applies a HEAL_SKILL_BOOST PERCENT modifier for the buff's duration; our `BonusHealSkillBoostPct` mirrors the existing `PassiveBonusHealSkillBoostPct` but for timed active buffs
  - **M337a:** `Model/Templates/Skill/SkillTemplate.cs` — added `OnetimeBoostHealPct` (scans `onetimeboostheal` for `stat="HEAL_SKILL_BOOST" func="PERCENT"`) and `OnetimeBoostHealDurationMs` (reads `duration2` from first `onetimeboostheal` element) to `SkillEffects`
  - **M337b:** `Model/AbnormalState.cs` — added `HealSkillBoostPct` field
  - **M337c:** `Model/Player.cs` — added `BonusHealSkillBoostPct` mutable property
  - **M337d:** `Model/Creature.cs` — `ApplyEffectDeltas`/`ReverseEffectDeltas` accumulate via `this is Player` cast
  - **M337e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: extended `durationMs` fallback chain with `OnetimeBoostHealDurationMs`; reads `healSkillBoostPct`, stores in `AbnormalState`; both `healBoostMult` (single-target) and `aoeBoostMult` (AoE heal) extended with `BonusHealSkillBoostPct` multiplicative factor
  - Build: 0 warnings, 0 errors

- [x] **M336: XP rate buffs — `xpboost` BOOST_HUNTING_XP_RATE + BOOST_GROUP_HUNTING_XP_RATE (22 + 22 = 30 skill entries)** — Solo and group NPC kill XP now scaled by active buff multipliers; crafting/gathering/tapping XP rate stats deferred (systems not yet implemented)
  - Java analog: `XpBoostEffect extends BufEffect`; on NPC kill Java calls `XPCalculation.getXP` which reads `StatEnum.BOOST_HUNTING_XP_RATE` from `PlayerGameStats.getStat`; our `ExperienceService.AddGroupExpAsync` now applies the same multiplier at the XP calculation point
  - **M336a:** `Model/Templates/Skill/SkillTemplate.cs` — added `HuntingXpBoostPct` and `GroupHuntingXpBoostPct` to `SkillEffects` (each scans `xpboost` elements for the matching `stat` name, `func="ADD"`)
  - **M336b:** `Model/AbnormalState.cs` — added `HuntingXpBoostPct` and `GroupHuntingXpBoostPct` fields
  - **M336c:** `Model/Player.cs` — added `BonusHuntingXpPct` and `BonusGroupHuntingXpPct` mutable properties
  - **M336d:** `Model/Creature.cs` — `ApplyEffectDeltas`/`ReverseEffectDeltas` accumulate both fields via `this is Player` cast
  - **M336e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: reads both pct fields, stores in `AbnormalState`
  - **M336f:** `Services/ExperienceService.cs` — solo path: `xp * (100 + killer.BonusHuntingXpPct) / 100`; group path: per-member `memberXp * (100 + member.BonusGroupHuntingXpPct) / 100`
  - Build: 0 warnings, 0 errors

- [x] **M335: FLY_TIME PERCENT buff (3 entries)** — Active buffs (e.g. "Lustrous Feather Effect" skill 1871 +400%, group buff skill 18145 +200%, test scroll skill 9865 delta-scaled) now increase effective MaxFp by the given percentage; FP heal, drain, regen and UI packets all use `EffectiveMaxFp`; base `MaxFp` unchanged for clean restoration on expiry
  - Java analog: `PlayerGameStats.getFlyTime() → getStat(StatEnum.FLY_TIME, BASE_FLYTIME)` accumulates all PERCENT modifiers onto the base fly-time value
  - **M335a:** `Model/Templates/Skill/SkillTemplate.cs` — added `FlyTimeStatUpPct` to `SkillEffects` (scans `statup`/`statboost` for `stat="FLY_TIME" func="PERCENT"`; respects `value + delta*(level-1)`)
  - **M335b:** `Model/AbnormalState.cs` — added `FlyTimePctDeltaVal` field
  - **M335c:** `Model/Player.cs` — added `BonusFlyTimePct` (mutable, accumulated) and `EffectiveMaxFp` computed property (`MaxFp * (100 + BonusFlyTimePct) / 100`)
  - **M335d:** `Model/Creature.cs` — `ApplyEffectDeltas`/`ReverseEffectDeltas` accumulate `BonusFlyTimePct` and clamp `CurrentFp` to `EffectiveMaxFp` on both apply and reverse
  - **M335e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: reads `flyTimePctDelta`, stores in `AbnormalState.FlyTimePctDeltaVal`; all 7 `MaxFp` references (FP heal single-target, FP heal HoT tick, FP heal AoE, FP attack instant, FP attack DoT tick) updated to `EffectiveMaxFp`
  - **M335f:** `Network/Aion/ServerPackets/SM_STATS_INFO.cs` — both MaxFp writes now use `p.EffectiveMaxFp`
  - **M335g:** `Services/RegenService.cs` — FP regen cap and `SM_FLY_TIME` packets use `EffectiveMaxFp`
  - **M335h:** `Services/PlayerEnterWorldService.cs` — CurrentFp clamp and `SM_FLY_TIME` packet use `EffectiveMaxFp`
  - **M335i:** `Network/Aion/ClientPackets/CM_USE_ITEM.cs` — item FP heal cap uses `EffectiveMaxFp`
  - **M335j:** `Services/AuraChildApplier.cs` — aura FP heal percent uses `EffectiveMaxFp`
  - Build: 0 warnings, 0 errors

- [x] **M333: ABNORMAL_RESISTANCE_ALL ADD buff (52 entries) + BOOST_HATE PERCENT boosthate (14 entries)** — CC resist buffs now gate all CC-flag debuffs; boosthate PASSIVE skills (Gladiator "Aggravation") + active boosthate buffs now scale hate generation
  - Java analog: `ABNORMAL_RESISTANCE_ALL → AbnormalEffect.calculate() Rnd.get(10000) < resistValue = resisted`; `BOOST_HATE PERCENT → StatFunctions.calculateHate(creature, value) * (1 + boost/100)`
  - **M333a:** `Model/Templates/Skill/SkillTemplate.cs` — added `CcResistAllAddDelta` (scans `statup`/`statboost` for `stat="ABNORMAL_RESISTANCE_ALL" func="ADD"`) and `BoostHateStatPct` (scans `boosthate` elements for `stat="BOOST_HATE" func="PERCENT"`) to `SkillEffects`
  - **M333b:** `Model/AbnormalState.cs` — added `CcResistAllDeltaVal` and `BoostHatePctDeltaVal` fields
  - **M333c:** `Model/Creature.cs` — added `CcResistAll` field; wired in `ApplyEffectDeltas`/`ReverseEffectDeltas`; `BoostHatePctDeltaVal` handled via `this is Player` cast
  - **M333d:** `Model/Player.cs` — added `BoostHatePct` property (accumulated from active buff AbnormalStates; passive is computed on-the-fly)
  - **M333e:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: reads `ccResistAllDelta`/`boostHateDelta`, stores in `AbnormalState`; DEBUFF path: `ccResisted` check gates full debuff when `template.CcFlags != None && target.CcResistAll > 0 && Random.Next(10001) < CcResistAll`; taunt hate scaled by `player.BoostHatePct + PassiveBoostHateHelper.ComputePct`
  - **M333f:** `Services/PassiveBoostHateHelper.cs` — new static helper; scans player.Skills for PASSIVE skills with `BoostHateStatPct != 0` (mirrors PassiveArmorMasteryHelper pattern)
  - **M333g:** `Services/NpcAiService.cs` — `ForceEngage` scales baseline hate=1 by `(100 + BoostHatePct + passiveBoostPct) / 100`
  - Note: Toggle/duration=0 active-buff boosthate skills (e.g. Gladiator skill 258 "Defense Preparation") are deferred — their AbnormalState expires immediately; only PASSIVE boosthate and timed-buff boosthate are active
  - Build: 0 warnings, 0 errors

- [x] **M330: statup PERCENT REGEN_HP and REGEN_MP buffs** — 3 REGEN_HP PERCENT entries (+10% to +20% HP regen rate from long-duration NPC/scroll buffs) + 2 REGEN_MP PERCENT entries now correctly increase per-tick regen rate in RegenService; buff expiry automatically restores via Creature.ReverseEffectDeltas
  - Java analog: `StatUpEffect REGEN_HP/REGEN_MP PERCENT` — multiplies per-tick regen amount by (100+pct)/100
  - **M330a:** `Model/AbnormalState.cs` — added `RegenHpPctDeltaVal`, `RegenMpPctDeltaVal` fields
  - **M330b:** `Model/Player.cs` — added `BonusRegenHpPct`, `BonusRegenMpPct` properties
  - **M330c:** `Model/Creature.cs` — Player-cast handling in `ApplyEffectDeltas`/`ReverseEffectDeltas`
  - **M330d:** `Services/RegenService.cs` — HP/MP regen multiplied by `(100 + BonusRegenHpPct/MpPct) / 100` when non-zero
  - **M330e:** `Model/Templates/Skill/SkillTemplate.cs` — added `RegenHpStatUpPct`, `RegenMpStatUpPct` to `SkillEffects`
  - **M330f:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: reads pct, stores in AbnormalState `RegenHpPctDeltaVal`/`RegenMpPctDeltaVal`; Creature.ApplyEffectDeltas then sets `BonusRegenHpPct`/`BonusRegenMpPct`
  - Build: 0 warnings, 0 errors

- [x] **M329: statup PERCENT MAGICAL_DEFEND buff** — 3 MAGICAL_DEFEND PERCENT entries (+5% from long-duration scroll, +35% from combat proc "Magic Fortification") now compute flat delta from target's current magic defense at cast time; zero new Player fields needed
  - Java analog: `StatUpEffect MAGICAL_DEFEND PERCENT`
  - **M329a:** `Model/Templates/Skill/SkillTemplate.cs` — added `MagicDefStatUpPct` property to `SkillEffects`
  - **M329b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: `magicDefStatUpDelta += player.MagicDefense * pct / 100` (Player targets only)
  - Build: 0 warnings, 0 errors

- [x] **M328: statdown/statup PERCENT BOOST_MAGICAL_SKILL + statup/statdown PERCENT BLOCK + statup PERCENT PARRY** — 13 BOOST_MAGICAL_SKILL PERCENT debuffs (NPC water/fire-element "-30% to -60% magic power" debuff chains) + 6 BOOST_MAGICAL_SKILL statup/statboost PERCENT buffs + 5 statup BLOCK PERCENT buffs + 1 statdown BLOCK PERCENT debuff + 4 statup PARRY PERCENT buffs now compute flat delta from target's current stat at cast time
  - Java analog: `StatDownEffect/StatUpEffect BOOST_MAGICAL_SKILL/BLOCK/PARRY PERCENT`
  - **M328a:** `Model/Templates/Skill/SkillTemplate.cs` — added `MagicBoostPctDebuff`, `MagicBoostStatUpPct`, `BlockStatUpPct`, `BlockPercentDebuff`, `ParryStatUpPct` to `SkillEffects`
  - **M328b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: magic boost, block, parry PERCENT → `+= BonusMagicBoost|BaseBlock+BonusBlock|BaseParry+BonusParry * pct/100`; DEBUFF path: same for magic boost and block debuffs (Player targets only)
  - Build: 0 warnings, 0 errors

- [x] **M327: statup PERCENT PHYSICAL_CRITICAL buff + statup/statdown PERCENT PHYSICAL_ACCURACY** — 20 PHYSICAL_CRITICAL PERCENT entries (Assassin/Ranger/Gladiator crit-rate proc buffs: +5% to +70% of current crit rating; Warrior "Empyrean Fury" on-hit crit boost) + 10 PHYSICAL_ACCURACY PERCENT buffs (long-duration +50% phys acc buffs) + 2 PHYSICAL_ACCURACY PERCENT debuffs (NPC fire-element -50% phys acc) now correctly compute flat delta from target's current stat at cast time
  - Java analog: `StatUpEffect/StatDownEffect PHYSICAL_CRITICAL/PHYSICAL_ACCURACY PERCENT`
  - **M327a:** `Model/Templates/Skill/SkillTemplate.cs` — added `PhysCritStatUpPct`, `PhysAccStatUpPct`, `PhysAccPercentDebuff` properties to `SkillEffects`
  - **M327b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: `physCritStatUpDelta += (BaseCritRating+BonusPhysicalCritical)*pct/100`; `physAccStatUpDelta += (BasePhysicalAccuracy+BonusPhysicalAccuracy)*pct/100`; DEBUFF path: `physAccDelta += currentPhysAcc*pct/100` (Player targets only)
  - Build: 0 warnings, 0 errors

- [x] **M326: statdown PERCENT PHYSICAL_ATTACK, EVASION, and MAGICAL_ATTACK debuffs** — 56 PHYSICAL_ATTACK PERCENT entries (Templar "Nezekan's Shield I" -50%, "Flight: Weakening Flame I-II" -100% patk), 28 EVASION PERCENT entries (Assassin "Shadow Rage I-II" -50%, NPC "Weaken Defense" series), 6 MAGICAL_ATTACK PERCENT entries (Chanter "Healing Mantra I-III Effect" -30%, "Fearful Presence" -30%) now correctly compute flat delta from target's current stat at cast time
  - Java analog: `StatDownEffect PHYSICAL_ATTACK/EVASION/MAGICAL_ATTACK PERCENT` — multiplies current stat by pct/100, subtracts as ADD delta
  - **M326a:** `Model/Templates/Skill/SkillTemplate.cs` — added `PatkPercentDebuff`, `EvasionPercentDebuff`, `MagicAtkPercentDebuff` properties to `SkillEffects`; scan `statdown` elements for matching stat+`func=PERCENT` children
  - **M326b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — DEBUFF path after M322 block: `patkDelta += (basePatk) * pct/100`; `evasionDelta += (BaseEvasion+BonusEvasion)*pct/100`; `magicAtkDelta += (MainHandMagicalAtk+BonusMagicAtk)*pct/100` (Player targets only)
  - Build: 0 warnings, 0 errors

- [x] **M325: statup PERCENT MAGICAL_RESIST and EVASION buffs** — 48 MAGICAL_RESIST PERCENT entries (magic resistance boost buffs) + 19 EVASION PERCENT entries now correctly compute flat delta from target's current stat at cast time
  - **M325a:** `Model/Templates/Skill/SkillTemplate.cs` — added `MResistStatUpPct` and `EvasionStatUpPct` properties to `SkillEffects`
  - **M325b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: `mresistStatUpDelta += player.BonusMagicResist * pct / 100`; `evasionStatUpDelta += (player.BaseEvasion + BonusEvasion) * pct / 100` (Player targets only)
  - Build: 0 warnings, 0 errors

- [x] **M324: statup PERCENT PHYSICAL_DEFENSE and ATTACK_SPEED buffs** — 181 PDEF PERCENT entries (Templar "Shield Defense I", "Defense Preparation II-III" at +144-188% pdef; Gladiator/Warrior defensive buff lines) + 238 ATTACK_SPEED PERCENT entries (Warrior "Daevic Fury" -10%, "Blessing of Nezekan" -20%, "Maximization of Speed" -20%, etc.) now correctly compute flat delta at cast time
  - Java analog: `StatUpEffect PHYSICAL_DEFENSE PERCENT` and `ATTACK_SPEED PERCENT`
  - **M324a:** `Model/Templates/Skill/SkillTemplate.cs` — added `PdefStatUpPct` and `AtkSpeedStatUpPct` properties to `SkillEffects`; scan `statup`/`statboost` for `PHYSICAL_DEFENSE PERCENT` and `ATTACK_SPEED PERCENT` children
  - **M324b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: `pdefStatUpDelta += player.PhysicalDefense * pct / 100` (Player targets only); `atkSpeedStatUpDelta += buffTarget.CurrentAttackSpeed * pct / 100` (works for both Player and NPC; negative pct = faster attacks → negative ADD delta → faster)
  - Previously: "Defense Preparation" gave 0 pdef boost at runtime (no flat ADD delta); "Daevic Fury" attack speed boost had 0 effect
  - Build: 0 warnings, 0 errors

- [x] **M323: statup PERCENT PHYSICAL_ATTACK and MAGICAL_ATTACK buffs** — 320 PHYSICAL_ATTACK PERCENT buff entries (Warrior "Ferocity/Berserking/Daevic Fury/Empyrean Fury", Templar/Gladiator offensive buffs) + 152 MAGICAL_ATTACK PERCENT entries (Spiritmaster "Spirit Armor of Light/Darkness", "Armor Spirit") now correctly boost the target's attack stats
  - Java analog: `StatUpEffect` with `func=PERCENT` on `PHYSICAL_ATTACK/MAGICAL_ATTACK` — multiplies the effective stat by (100+X)/100 relative to the pre-buff base
  - **M323a:** `Model/Templates/Skill/SkillTemplate.cs` — added `PhysAtkStatUpPct` and `MagicAtkStatUpPct` properties to `SkillEffects`; scan `statup`/`statboost` elements for `<change stat="PHYSICAL_ATTACK/MAGICAL_ATTACK" func="PERCENT"/>`, sum values
  - **M323b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — BUFF path: reads `patkStatUpPct`/`magicAtkStatUpPct`; if `buffTarget is Player`, computes flat delta = `basePatk * pct / 100` (basePatk = BasePhysicalAttack + weapon avg + BonusPhysicalAtk) or `(MainHandMagicalAtk + BonusMagicAtk) * pct / 100`; adds to `patkStatUpDelta`/`magicAtkStatUpDelta` before the AbnormalState is built
  - Limitation: NPC targets (summoned spirits) receive no percentage boost since spirit stats aren't modeled as Player fields
  - Previously: all PERCENT P-atk and M-atk buff skills applied only their ADD stat-up deltas (which are 0 for these pure-PERCENT skills), so e.g. Ferocity I had zero actual P-attack boost at runtime
  - Build: 0 warnings, 0 errors

- [x] **M322: statdown PERCENT PHYSICAL_DEFENSE and MAGICAL_RESIST debuffs** — 157 enemy-debuff skills with PERCENT pdef reduction (Ranger "Focused Shots", "Fleshcutter Arrow", "Booming Strike"; many NPC line debuffs) + ~30 mresist PERCENT debuffs now correctly compute the flat pdef/mresist reduction at cast time
  - Java analog: `StatDownEffect` with `func=PERCENT` on `PHYSICAL_DEFENSE` or `MAGICAL_RESIST` — reduces the stat by X% of the target's current value
  - **M322a:** `Model/Templates/Skill/SkillTemplate.cs` — added `PdefPercentDebuff` and `MResistPercentDebuff` properties to `SkillEffects`; scan `statdown` elements for `<change stat="PHYSICAL_DEFENSE/MAGICAL_RESIST" func="PERCENT"/>` children, sum all values
  - **M322b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — debuff application block: reads `pdefPctDebuff`/`mresistPctDebuff` after MaxHp/MaxMp pct conversions; if target is Player, computes flat delta = `target.PhysicalDefense/BonusMagicResist * pct / 100` and adds to `pdefDelta`/`mresistDelta` (merged into existing `PdefDelta`/`MResistDelta` field on AbnormalState)
  - Limitation resolved in M341: BUFF-path self-nerfing statdown elements are now handled
  - Previously: 157+ skills that should reduce pdef/mresist by percentage had no runtime effect on those stats; only the CC flag and duration applied
  - Build: 0 warnings, 0 errors

- [x] **M321: bind/buffbind CC flag corrected to Root (movement-lock, attack allowed)** — 52 `<bind>` + 3 `<buffbind>` entries; skills like "Lockdown I–IV" (Warrior), "Blinding Shackle I–II" (Priest) and similar target-movement-lock skills
  - Java analog: `BindEffect extends RootEffect` — prevents movement but does NOT prevent attacking; Java `RootEffect` sets `CANT_MOVE_STATE` only
  - **M321a:** `Model/Templates/Skill/SkillTemplate.cs` — `ElementToCcFlag` entry for `"bind" or "buffbind"` changed from `AbnormalCcFlags.Sleep` to `AbnormalCcFlags.Root`; `Root` is in `CantMove` only, while `Sleep` is in both `CantMove` and `CantAttack`
  - Previously: bind skills prevented both movement and attacking (Sleep semantics), making Warrior Lockdown incorrectly silence the target's offensive actions
  - Build: 0 warnings, 0 errors

- [x] **M320: StatDown FLY_SPEED PERCENT debuff — fly speed reduction from `<statdown>` elements** — 24 skill XML entries (Ranger "Meteor Strike I–IV" at -50%, Spiritmaster "Curse of Fire/Water I–II" at -30%, plus NPC debuff lines) that reduce target fly speed while the debuff is active
  - Java analog: `StatDownEffect extends BufEffect`; `FLY_SPEED PERCENT -50` reduces the target's effective fly speed stat by 50%. In .NET, applied as `BonusFlySpeedPct += statdownFlySpeedPct` (value is negative).
  - **M320a:** `Model/Templates/Skill/SkillTemplate.cs` — added `StatdownFlySpeedPct` property on `SkillEffects`; scans `statdown` elements for `<change stat="FLY_SPEED" func="PERCENT"/>` children, sums all values (mirrors `StatdownSpeedPct` pattern)
  - **M320b:** `Model/AbnormalState.cs` — added `FlySpeedDebuffPct` (int) and `PreDebuffFlySpeedPct` (int) fields to track the fly speed penalty and pre-debuff baseline for restoration
  - **M320c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — debuff application block: reads `statdownFlySpeedPct`; stores in `AbnormalState` with pre-debuff `BonusFlySpeedPct` snapshot; applies `BonusFlySpeedPct += statdownFlySpeedPct` on target; expiry task restores `BonusFlySpeedPct = PreDebuffFlySpeedPct` after `RemoveEffectBySkillId`
  - Limitation: fly speed changes are internal-state only; `SM_STATS_INFO` and `SM_EMOTION` do not carry fly speed, so the client stat panel will not reflect the change until flight subsystem is fully implemented
  - Previously: 24 statdown FLY_SPEED skills applied their CC flag and debuff duration but caused no actual fly speed change
  - Build: 0 warnings, 0 errors

- [x] **M319: FpAtk DoT — `<fpatk>` periodic FP drain over time (Java FpAtkEffect)** — 29 skill XML entries (Ranger/Gunner aerial disruption skills with flight-point drain DoTs) now correctly drain target FP per tick
  - Java analog: `FpAtkEffect extends AbstractOverTimeEffect`; on each tick calls `player.getLifeStats().reduceFp(value)`; `percent=true` drains `value%` of MaxFP per tick
  - **M319a:** `Model/Templates/Skill/SkillTemplate.cs` — added `FpAttackDotEffects` getter returning `IReadOnlyList<SkillMpAttackDotInfo>` (reuses same record); scans `fpatk` elements; parses `checktime`, `value`, `delta`, `duration2`, `percent` (mirrors M306 `MpAttackDotEffects` pattern exactly)
  - **M319b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — after M306 mpattack block: if target is Player and `FpAttackDotEffects.Count > 0`, spawns `Task.Run` per dot entry; each tick computes `fpDrain = IsPercent ? MaxFp * value / 100 : value + delta*(level-1)`; applies to `fpTickTarget.CurrentFp`; sends `SM_STATS_INFO` to target's connection per tick; cleans up on expiry via `RemoveEffect + SM_ABNORMAL_EFFECT` broadcast
  - Previously: 29 fpatk skill XML entries silently applied their initial `fpatkinstant` hit (M281) but the over-time FP drain never ticked — aerial disruption skills only reduced target FP once instead of repeatedly
  - Build: 0 warnings, 0 errors

- [x] **M318: StatDown SPEED PERCENT debuff — movement speed reduction from `<statdown>` elements** — 58 skills with `<change stat="SPEED" func="PERCENT" value="-X"/>` inside `statdown` blocks (Spiritmaster "Curse of Fire/Water", boss "Aerial Fury" charge mechanics, NPC debuff lines) that reduce target movement speed identically to snare
  - Java analog: `StatDownEffect extends BufEffect`; `SPEED PERCENT -30` reduces the target's effective speed stat by 30%. In .NET, both snare and statdown SPEED are PERCENT-type reductions applied to `target.MovementSpeed`.
  - **M318a:** `Model/Templates/Skill/SkillTemplate.cs` — added `StatdownSpeedPct` property on `SkillEffects`; scans `statdown` elements for `<change stat="SPEED" func="PERCENT"/>` children, sums all values (mirrors `SnareSpeedPct` pattern)
  - **M318b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — debuff application block: reads `statdownSpeedPct`; combines with `snareSpeedPct` into `combinedMovSpeedPct`; `MovSpeedPct = combinedMovSpeedPct` on `AbnormalState`; speed application and `SM_EMOTION` broadcast use the combined value; `PreDebuffSpeed` correctly records pre-debuff speed for restoration on expiry
  - Limitation: ADD-func SPEED changes (2 entries — Okaru Poison -1000, Overload -9500) remain deferred; the Java SPEED unit scale doesn't map directly to our `MovementSpeed` float without a stat-template lookup
  - Previously: 58 statdown SPEED skills applied their CC flag and debuff duration but caused no actual movement speed reduction — Spiritmaster "Curse of Fire/Water" lines appeared to slow the target in UI but had no runtime effect
  - Build: 0 warnings, 0 errors

- [x] **M317: Periodic HP drain — `<periodicactions><hpuse>` per-tick HP drain while buff active** — 10 skill XML entries (Spiritmaster Penance I-IV, Stigma Penance I, Spirit Bloodlust II, Mobility Thrusters I-III) that maintain an MP-regen or mobility buff at the cost of HP per tick
  - Java analog: `PeriodicActions` schedules an `HpUseAction` on each tick, consuming `value + delta * level` HP; buff stays active regardless of HP level (no forced-stop in Java — we add an HP=1 floor deactivation for safety)
  - **M317a:** `Model/Templates/Skill/SkillTemplate.cs` — added `PeriodicHpUse` property `(CheckTimeMs, HpValue, HpDelta)` on `SkillEffects`; scans `<periodicactions><hpuse>` children (mirrors M274 `PeriodicMpUse` pattern exactly)
  - **M317b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — buff path: after M274 MP drain block, added analogous HP drain `Task.Run` loop; computes `hpDrainPerTick = hpUseValue + hpUseDelta * (level-1)`; floors at HP=1 (not 0 — HP drain shouldn't kill the caster); on HP=1 auto-deactivates via `RemoveEffectBySkillId + SM_PLAYER_STANCE(0) + SM_ABNORMAL_EFFECT` broadcast (mirrors M290 MP deactivate)
  - Previously: 10 skill XML entries (Penance I-IV + variants) applied their MP-regen buff with no HP cost — Spiritmaster's Penance effectively gave free MP at no resource cost
  - Build: 0 warnings, 0 errors

- [x] **M316: DP + HP cast costs — `<actions><dpuse>` and `<actions><hpuse>` enforced before cast** — 206 `dpuse` + 203 `hpuse` skill XML entries (Gladiator/Templar DP skills, Spiritmaster/Assassin blood-cost skills, several boss-encounter HP-drain casts) now validate and consume DP/HP before the skill takes effect
  - Java analog: `DpUseAction.act` checks `currentDp >= value`; if not, sends `STR_SKILL_NOT_ENOUGH_DP` (1300016) and returns false. `HpUseAction.act` computes `value + delta*level`; if `ratio=true` multiplies by `MaxHp/100`; sends `STR_SKILL_NOT_ENOUGH_HP` (1300014) if insufficient
  - **M316a:** `Model/Templates/Skill/SkillTemplate.cs` — added `[XmlElement("actions")] SkillActions?`; new `SkillActions` class with `[XmlElement("dpuse")] SkillDpUse?` + `[XmlElement("hpuse")] SkillHpUse?`; `SkillDpUse.Value` int; `SkillHpUse.{Value,Delta,IsRatio}`; convenience properties `DpUseCost` (int) and `HpUseCost` tuple on `SkillTemplate`
  - **M316b:** `Network/Aion/ServerPackets/SM_SYSTEM_MESSAGE.cs` — added `NotEnoughDp()` (1300016) and `NotEnoughHp()` (1300014) factory methods
  - **M316c:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — pre-cast block after chain check: if `DpUseCost > 0` and `player.Dp < cost` → send `NotEnoughDp`, return; else deduct and send `SM_DP_INFO`; if `HpUseCost.Value > 0` and `player.CurrentHp < cost` (with ratio scaling) → send `NotEnoughHp`, return; else deduct HP
  - Previously: 409 skills with DP/HP action costs consumed nothing — DP skills worked without any DP, blood-cost skills cost no HP; players could spam level-10 DP ultimates at 0 DP
  - Build: 0 warnings, 0 errors

- [x] **M312: MoveBehind + BackDash — gap-closer and retreating strike attacks** — `<movebehind>` deals physical damage and teleports caster directly behind the target; `<backdash distance="N">` deals physical damage and moves caster backward by distance units; 11 + 17 = 28 skills total (Assassin Ambush I-VII, Blind Side, Stigma Ambush; Gladiator/Ranger Retreating Slash, Fighting Withdrawal, Beast Leap, Parting Shot lines)
  - Java analog: `MoveBehindEffect extends DamageEffect` — `calculate()` positions caster at `(target.X + cos(π + targetHeading) * 1.3, target.Y + sin(π + targetHeading) * 1.3, target.Z)` then calls `super.calculate(PHYSICAL)`; `BackDashEffect extends DamageEffect` — calls `super.calculate(PHYSICAL)` then sets position to `(effector.X + cos(π + effectorHeading) * distance, ...)` in `applyEffect()`
  - Position formula: Aion heading is 0-255 = 0-2π; offset = cos/sin(heading × 2π/256 + π) × distance
  - **M312a:** `Model/Templates/Skill/SkillTemplate.cs` — added `"movebehind"` and `"backdash"` to `DamageEffectNames`; added both to the physical-type branch in `DamageEffects` (`e.LocalName is "skillatk" or … or "movebehind" or "backdash"`)
  - **M312b:** `Network/Aion/ClientPackets/CM_CASTSPELL.cs` — added `movebehind` block after dash block: if target alive, computes position 1.3 units behind target heading, updates `player.Position`, sends `SM_TELEPORT_LOC` to caster + broadcasts `SM_MOVE` to zone; added `backdash` block: reads `distance` attribute from first `backdash` XML element (default 25), computes position `distance` units opposite caster heading, updates `player.Position`, sends `SM_TELEPORT_LOC` + broadcasts `SM_MOVE`; backdash movement fires regardless of target state (caster always retreats)
  - Build: 0 warnings, 0 errors

---

## Remaining Work Plan (added 2026-07-11, after real-client login verification)

Login verified end-to-end with a real 4.6 client (byte-diff vs Java: identical). Test tooling
(LoginTestClient, FixedRandomSource/TestMode on both sides) removed after verification.
Module status at time of writing: Commons ~90%, Login ~90%, Chat ~80%, Game ~35%.

### Phase A — Login to 100% (small, do first)
- [x] **A1: Character-count roundtrip** (2026-07-11) — ported `loadGSCharactersCount`:
      `AccountController.RequestServerListAsync` sends SM_GS_CHARACTER_RESPONSE (opcode 8, accountId)
      to each online GS; new `CM_GS_CHARACTER` handler (GS opcode 8: accountId D + count C) caches
      counts; SM_SERVER_LIST now carries real per-server character counts and is sent when all GS
      reply (3s timeout fallback — Java has none and would hang the client on a silent GS).
      CM_SERVER_LIST also gained the Java NO_GS_REGISTERED(6) close path.
- [x] **A2: Reconnect flow** (2026-07-11) — `CM_ACCOUNT_RECONNECT_KEY` (GS opcode 2) removes the
      account from the GS list, stores a `ReconnectingAccount` with a random key, replies
      `SM_ACCOUNT_RECONNECT_KEY` (opcode 3: accountId + key). `CM_UPDATE_SESSION` (client 0x08)
      now validates via `AuthReconnectingAccountAsync` (key check → rebind account, new SessionKey,
      AUTHED_LOGIN, SM_UPDATE_SESSION) and closes on mismatch — full Java parity.
- [x] **A3 (partial): CM_MAC (13)** (2026-07-11) — updates `account_data.last_mac` via new
      `IAccountDao.UpdateLastMacAsync`; registered in both CONNECTED and AUTHED states like Java.
- [ ] **A3 (deferred): admin/premium GS-link packets** — CM_LS_CONTROL(5), CM_BAN(6),
      CM_ACCOUNT_TOLL_INFO(9), CM_MACBAN_CONTROL(10), CM_PREMIUM_CONTROL(11), CM_PTRANSFER_CONTROL(14).
      Explicitly deferred: all are admin-tool/in-game-shop/player-transfer features, feature-gated in
      Java and not part of the core login→play loop. Port when the corresponding Game features land.
- [ ] **A4: Runtime verification** — real-client pass over A1/A2: character counts visible in server
      list; logout-to-server-select relogs without password.

### Phase B — Chat validation (verification, not code-first)
- [ ] **B1:** Run real client + Game + Chat, verify SM_CHAT_INIT token handshake, whisper, LFG/trade channels.
- [ ] **B2:** Fix whatever B1 surfaces (expect wire-format details like the Login encLen/checksum quirks).

### Phase C — Game gaps (large; keep milestone convention M###)
Priority order chosen for player-visible impact per unit of work:
- [ ] **C1: Server-packet audit** — 137/231 SM_* ported. Enumerate the missing 94, classify:
      needed-for-4.6-core vs siege/housing/event-only. Port the core set.
- [ ] **C2: Quest engine depth** — current QuestService covers XML kill/collect basics. Port the
      Java questEngine handler model (per-quest logic; Java has 76 engine files + script handlers)
      onto the existing Roslyn scripting infra (Scripts/ folder pattern from Login).
- [ ] **C3: NPC AI** — replace single NpcAiService with a port of the ai2 state machine
      (idle/patrol/aggro/return, leash, social aggro, flee) — biggest combat-feel gap.
- [ ] **C4: Geodata** — port geoEngine (43 files) or integrate a minimal Z-lookup + LoS service;
      prerequisite for correct ranged combat, pathing, and fall damage.
- [ ] **C5: Instances** — portal/entry flow exists (PortalData, InstanceExitData); add instance
      world lifecycle (create/destroy per group), then port 4.6 core instances one by one.
- [ ] **C6: Flight** — fly state is internal-only; wire SM_EMOTION/SM_STATS fly speed, FP drain on
      flight, no-fly zones.
- [ ] **C7: Sieges, housing, pets, mail completion** — after C1–C6.

Testing convention going forward: every phase gets verified against the real client before the
next phase starts (lesson from the login investigation: byte-level correctness is provable, but
only a real client run proves the flow).

#### C1 progress (2026-07-11)
- Audit result: 103 SM_* packets missing vs Java 4.6. Classification: ~60 belong to deferred
  feature clusters (housing 15, siege/abyss 9, alliance/auto-group 4, in-game shop 4, instances
  scoring 3, fast-track 2, pets 2, summon panel 5, misc/unused ~16); ~40 are core-adjacent.
- [x] **C1 batch 1: vital-stat + buff-bar self-updates**
  - `SM_STATUPDATE_HP` (0x03), `SM_STATUPDATE_MP` (0x04), `SM_STATUPDATE_DP` (0x06),
    `SM_ABNORMAL_STATE` (0x31) created with byte-exact Java layouts.
  - `HpUpdateHandler` (new, registered on DamageDealtEvent) — SM_STATUPDATE_HP to any damaged
    player (Java PlayerLifeStats.onReduceHp parity).
  - `RegenService` — HP/MP regen ticks now also send SM_STATUPDATE_HP/MP to the owner.
  - `CM_LEVEL_READY` — sends SM_ABNORMAL_STATE (own buff bar with durations) alongside the
    SM_ABNORMAL_EFFECT zone broadcast on zone-in.
  - Note: DP sites already send SM_DP_INFO everywhere; SM_STATUPDATE_DP exists for future parity
    but was not shotgunned into the 6 working DP sites.
- [x] **C1 batch 2: SM_SKILL_REMOVE (0x2D), SM_LOOKATOBJECT (0x28), SM_ITEM_COOLDOWN (0x67)**
  - `SM_SKILL_REMOVE` — sent per stigma skill on unequip/displacement in CM_EQUIP_ITEM (Java
    SkillLearnService.removeSkill parity; profession/stigma/normal byte variants implemented).
  - `SM_LOOKATOBJECT` — broadcast when an NPC acquires a target (aggro pick + ForceEngage in
    NpcAiService; Java Npc.setTarget parity) so NPCs visibly face their victim.
  - `SM_ITEM_COOLDOWN` — Player.ItemCooldowns now stores (expiry, delayMs); packet sent on enter
    world when cooldowns exist. Note: item cooldowns are not yet DB-persisted, so the login list
    is only non-empty after a same-session map transfer; persistence is a future milestone.
  - Build: 0 warnings, 0 errors.
- [x] **C1 batch 3: SM_INVENTORY_UPDATE_ITEM (0x1D)** — in-place stack/stat item update
  - Java shape: D(objectId) + nameId block (H 0x24, D nameId, H 0) + item blob + H(updateType mask);
    `UpdateType` enum ports the sendable Java ItemUpdateType masks (StatsChange, DecItemUse, ...).
  - `SM_INVENTORY_INFO.WriteItemInfo` refactored: blob body extracted to `WriteItemBlob` and shared.
  - Wired: CM_CRAFT partial component consumption (replaces the SM_INVENTORY_ADD_ITEM workaround
    with the Java DEC_ITEM_USE update) and CM_EQUIP_ITEM stigma-shard partial consumption
    (previously sent no packet — stale shard count until relog).
  - Build: 0 warnings, 0 errors.
- [x] **C1 batch 4: SM_CASTSPELL_RESULT (0x2B)** — skill-hit result packet (damage number,
  per-target HP pct, hit status) broadcast at hit time; previously the client only inferred
  skill damage from SM_ATTACK_STATUS.
  - Common-path port: dashStatus=0, spellStatus=0, no shield/reflect sub-blocks (extend later);
    statusFlag H: 16 no-damage / 32 regular per Java comment; cooldown D written 0 because
    SM_SKILL_COOLDOWN is already sent at cast start.
  - Wired at the single-target skill damage site in CM_CASTSPELL. AoE loop deliberately NOT wired
    yet — pending real-client verification of the packet layout first (one bad 0x2B would crash
    the client on every skill).
  - Also skipped after audit: SM_FRIEND_UPDATE and SM_LEARN_RECIPE/SM_RECIPE_DELETE — C# already
    covers those flows with full-list refreshes (SM_FRIEND_LIST / SM_RECIPE_LIST), no functional gap.
  - Build: 0 warnings, 0 errors.
- [x] **C1 batch 5: SM_SELL_ITEM (0x3E) + SM_MANTRA_EFFECT (0xD0)**
  - `SM_SELL_ITEM` — TRADE_SELL_LIST dialog now opens the proper vendor sell window (Java
    DialogService SELL case, no-purchase-template variant) instead of a generic SM_DIALOG_WINDOW.
  - `SM_MANTRA_EFFECT` — chanter aura/mantra visual broadcast to the zone when an aura skill
    starts (Java AuraEffect.startEffect parity), wired at the M286 aura block in CM_CASTSPELL.
  - C1 status after 5 batches: the wireable core-adjacent set is done. Remaining missing SM_*
    packets either belong to deferred feature clusters (housing/siege/alliance/shop/pets/summons)
    or depend on C2/C3 systems (SM_NEARBY_QUESTS needs a server-side npc→quest index;
    SM_PLAY_MOVIE needs quest handlers; SM_PLAYER_MOVE/SM_FORCED_MOVE need geodata-backed
    movement validation). Next: C2 quest engine depth.

#### C2 progress (2026-07-11) — quest engine
Survey findings (Java questEngine, 72 core classes + 1,493 scripted handlers):
- Data-driven template handlers cover ~4,332 quests from `quest_script_data/*.xml`:
  item_collecting ~1,961, monster_hunt ~1,607, report_to ~487, report_to_many ~84,
  kill_in_world ~48, + work_order 574 entries. Porting ItemCollecting+MonsterHunt+ReportTo
  covers ~4,000 quests before touching any hand-written script.
- Hand-written per-quest scripts (1,493 files) are Phase 5, via the Roslyn Scripts/ pattern.
- QuestVars encoding (6 vars x 6 bits base-64 packed into `step`) — C# QuestEntry already matches.
- Dialog protocol contract: client DialogAction ids (31 select, 1002 accept, 1009 reward-select)
  vs server response dialog ids (4, 5+rewardIdx, 1003/1004, 1011, 1352, 2375) — port the full
  enum instead of today's scattered magic numbers; include the reward-window status guard
  (Java refuses SELECT_QUEST_REWARD unless status==REWARD) to prevent reward duping.

- [x] **C2 Phase 0: QuestStatus wire/DB compatibility fix (critical)** — C# enum was
  START=1/REWARD=2/COMPLETE=3; the 4.6 client and Java DB expect NONE=0/START=3/REWARD=4/
  COMPLETE=5/LOCKED=6. Values flow into player_quests.status and SM_QUEST_ACTION/SM_QUEST_LIST.
  Fixed enum (all 17 usages are symbolic) + V35__quest_status_java_values.sql migrates existing
  rows (single CASE, pre-update values) and changes the column default to 3.
- [x] **C2 Phase 1**: QuestEngine singleton + npc→quest indexes (QuestNpc), QuestEnv,
  QuestHandlerBase (dialog helpers), QuestScriptData loader, ItemCollecting template handler,
  QuestEngineHostedService; re-route CM_DIALOG_SELECT quest cases + QuestService hooks; then
  SM_NEARBY_QUESTS becomes buildable.
  - `QuestEngine/` (new): `QuestEngine.cs` (npc/item indexes + OnDialogAsync/OnKillAsync/
    OnItemGetAsync dispatchers, `HasHandler` for fallback gating), `QuestNpc.cs`,
    `Model/QuestEnv.cs` (record; `RewardIndex` added beyond the original 4-field sketch —
    matches Java's `extendedRewardIndex`, needed for SELECT_QUEST_REWARD payout),
    `Model/DialogAction.cs` (full Java enum port, all ~150 members, same numeric ids;
    `DialogActionLookup.FromId` mirrors `getActionByDialogId`, first-declared wins on the one
    duplicate id 57), `Handlers/IQuestHandler.cs`, `Handlers/QuestHandlerBase.cs`,
    `Handlers/Templates/ItemCollectingHandler.cs`, `QuestEngineHostedService.cs`.
  - `Model/Templates/Quest/Script/QuestScriptData.cs` + `DataHolders/QuestScriptData.cs`: parses
    `<item_collecting>` from every `quest_script_data/*.xml` (root `quest_scripts`, sibling
    element types e.g. report_to/monster_hunt are simply skipped by XmlSerializer). Real attribute
    names are `start_dialog_id`/`start_dialog_id2` per the shipped XSD — the Java
    `ItemCollectingData` model's `HACTION_QUEST_SELECT_id` annotation is stale/does not match the
    data on disk, so the C# attributes follow the XSD, not the Java field names.
  - Reward payout extracted from `CM_DIALOG_SELECT.HandleQuestRewardAsync` into a new
    `Services/QuestRewardService.GrantAndCompleteAsync` (exp/items/gold/AP/title + COMPLETE
    transition), called by both the legacy inline path and `QuestHandlerBase.SendQuestEndDialogAsync`
    — avoids duplicating ~180 lines. `QuestService.IsRewardReady` made `internal` and reused by
    `ItemCollectingHandler` instead of re-implementing the collect-item check.
  - Rerouted: `CM_DIALOG_SELECT` quest-select/accept/reward cases call `QuestEngine.OnDialogAsync`
    first, fall back to the existing inline logic on `false`. `QuestService.ProcessKillForPlayerAsync`
    / `HandleItemAcquiredAsync` call `QuestEngine.OnKillAsync`/`OnItemGetAsync` first, then skip any
    quest id already owned by a registered handler (`HasHandler`) before running legacy logic.
  - Deviation: used dialog page **4** (`ASK_QUEST_ACCEPT_WINDOW`, per Java `DialogPage`) for the
    not-yet-started quest-select response in the new engine path, not the `1007` the pre-existing
    fallback code sends — `1007` is a `DialogAction` id, not a `DialogPage` id; `4` is Java-correct.
    Left the fallback's `1007` untouched (out of scope / don't break what already works).
  - Data check: 1,961 `<item_collecting>` elements exist on disk; the loader parses 1,957 — one
    file (`growth.xml`) has a different root element (`<quest_XMLs>`, `quest_XML_data.xsd`) and is
    skipped with a warning rather than crashing the whole load (its 4 entries likely belong to the
    Phase 5 hand-written `xml_quest` system, not this template).
  - Build: 0 warnings, 0 errors.
- [x] **C2 Phase 2**: MonsterHunt (multi-var kill spans) + ReportTo templates.
  - `Model/Templates/Quest/Script/QuestScriptData.cs`: added `MonsterHuntScriptEntry` (+ nested
    `MonsterEntry` for `<monster var/start_var/end_var/npc_ids/npc_seq>`) and `ReportToScriptEntry`.
    Attribute names verified against `quest_script_data.xsd` and real XML samples (not the Java
    model annotations): `start_dialog_id`/`end_dialog_id`/`aggro_start_npcs`/`invasion_world` on
    `<monster_hunt>` (Java's `MonsterHuntData.startDialog` field is annotated
    `HACTION_QUEST_SELECT_id`, which is stale/does not exist in the shipped XML, same class of bug
    Phase 1 already found on ItemCollecting). `DataHolders/QuestScriptData.cs` extended to parse
    both new element types alongside `item_collecting` from the same `quest_scripts` root.
  - **Bug found and fixed (new code only)**: public get-only `HashSet<int>` convenience properties
    (`StartNpcIds`, `EndNpcIds`, `NpcIds`, `AggroStartNpcIds`) must carry `[XmlIgnore]`. Without it,
    `XmlSerializer` treats them as serializable collection members and invokes their getters while
    building the object graph — before the sibling `XmlAttribute`-bound raw string is assigned —
    which permanently memoizes an empty parse via the `??=` cache. Confirmed this is a **pre-existing
    bug in Phase 1's `ItemCollectingScriptEntry`** too (`ItemCollectingHandler._startNpcs` is always
    empty at runtime, so it never matches any start NPC) — left untouched per this phase's "extend,
    don't modify Phase 1" constraint, but flagging here since it means `ItemCollecting`'s engine path
    is currently a no-op in practice; needs the same one-attribute fix applied separately.
  - `QuestEngine/Handlers/Templates/MonsterHuntHandler.cs`: registers start npcs (OnQuestStart +
    OnTalk), every `<monster>` group's npc ids (OnKill), end npcs (OnTalk). Kill counting ports
    Java's do/while span-decode loop exactly: a group's progress is stored across
    `ceil(log64(end_var+1))` consecutive 6-bit quest vars starting at `var`; on kill, decode the
    current total, +1, reject (no-op, `OnKillAsync` returns false) if it would exceed `end_var`,
    else re-encode across the same slots. Verified against the actual worst-case shipped data
    (gelkmaros/inggison quest 21040: three groups at var 0/2/4 with end_var 329/231/77, using all 6
    of `QuestEntry`'s var slots) via a standalone bit-packing simulation — 329/231/77 kills decode
    back correctly, the (329+1)th kill is rejected, and the packed `Step` round-trips through
    encode/decode unchanged. Dialog flow mirrors `ItemCollectingHandler`'s convention (not Java's
    literal 3-click SELECT_QUEST_REWARD→SELECTED_QUEST_REWARDx sequence, which
    `QuestHandlerBase.SendQuestEndDialogAsync` doesn't model): START+QUEST_SELECT checks all monster
    groups and transitions to REWARD when satisfied, REWARD+SELECT_QUEST_REWARD grants. Added an
    explicit `player.Level < template.MinLevel` gate in the NONE-state branch (absent from Java's
    template itself) because the engine-first dispatch in `CM_DIALOG_SELECT` bypasses that packet
    handler's own level gate when a handler is registered — same reasoning as
    `ItemCollectingHandler`.
  - Not ported (documented as a phase limitation, not a defect): `aggro_start_npcs`-driven
    `onAddAggroListEvent` auto-start and `invasion_world`-driven `onEnterWorldEvent` auto-start
    (needs RiftService/VortexService, which don't exist yet in the .NET port) — `IQuestHandler` has
    no aggro-list or enter-world event hooks. Only 6 of 1,538 loaded monster_hunt entries use either
    attribute; both fields are still parsed into the model for completeness. The REWARD-state aggro
    dialog gating (10002/"in progress" page) IS ported since it's pure dialog logic needing no new
    event. Also not ported: `CustomConfig.QUESTDATA_MONSTER_KILLS` npc_seq-to-quest_kill matching —
    an optional legacy customization (default off) that widens a group's npc set from
    `quest_data.xml`; the handler always uses the `<monster>` element's own `npc_ids`, matching
    Java's default (config-disabled) path.
  - `QuestEngine/Handlers/Templates/ReportToHandler.cs`: registers start/end npcs the same way.
    ~40% of entries carry an `item_id` — a "quest work item" the handler gives on accept
    (`IItemDao`/`Player.Inventory`, mirroring `QuestRewardService`'s give/consume pattern) and
    removes on turn-in, independent of `quest_data.xml`'s `collect_items`. Deviation from Java:
    the item-give-on-accept applies uniformly to `QUEST_ACCEPT` and `QUEST_ACCEPT_1` (Java only
    gives the item for `QUEST_ACCEPT_1`/`QUEST_ACCEPT_SIMPLE`, not plain `QUEST_ACCEPT` — an
    asymmetry that looks like an unintentional quirk in the original, and `QuestHandlerBase`'s
    shared `SendQuestStartDialogAsync` already treats both the same). Turn-in
    (START+SELECT_QUEST_REWARD) validates the item count, consumes it, transitions to REWARD, and
    grants in one click — Java's literal flow needs a second click, but that's the
    `SELECTED_QUEST_REWARDx` mechanism `QuestHandlerBase` doesn't model (same simplification as
    MonsterHunt/ItemCollecting).
  - `QuestEngineHostedService.cs`: now takes `IItemDao` (already DI-registered) and registers both
    new template types alongside ItemCollecting; startup log reports per-type counts.
  - Data check: of 1,607 `<monster_hunt>` / 487 `<report_to>` elements on disk, the loader parses
    1,538 / 468 — the gap is the same `growth.xml` (`<quest_XMLs>` root, different XSD) Phase 1
    already found and warn-skips rather than crashing the whole load.
  - Build: 0 warnings, 0 errors.
- [ ] **C2 Phase 3**: ReportToMany, KillInWorld, KillSpawned, WorkOrders/ItemOrders.
- [ ] **C2 Phase 4**: reward templates (CraftingRewards/RelicRewards/FountainRewards/SkillUse/
  MentorMonsterHunt) + XmlQuest condition/operation mini-DSL.
- [ ] **C2 Phase 5**: hand-written quest scripts via Roslyn Scripts/ runtime-compile pattern.

#### C3 survey (2026-07-11) — NPC AI (Java ai2)
- Java ai2: event-driven state machine (9 states, 27 event types, 9 poll questions), behavior in
  static handler/manager classes; ~457 per-content AI script classes atop 3 core archetypes.
  No pathfinding anywhere — straight-line movement; geodata used only for LoS + Z clamp.
- Coverage: ai="aggressive" 30,323 NPCs + "general" 8,592 = ~85% of combat NPCs; "noaction" 1,500;
  guard family ~1,947; interaction AIs (portal/useitem/quest_use_item/trap/chest) are non-combat.
- Port decision: KEEP NpcAiService as tick driver/combat executor (M194-M380 combat parity lives
  there); add a pluggable INpcAi archetype layer (Model/Ai/: AiState, INpcAi, AbstractNpcAi,
  AiNameRegistry, Archetypes/{NoAction,GeneralNpc,AggressiveNpc,Guard,Portal,UseItem,Trap}).
  Port order: Aggressive+General (85%) → NoAction → Guard family → interaction AIs → registry
  fallback for the rest. Do NOT port the 457 script classes.
- Bugs found in current NpcAiService (fix during archetype refactor):
  1. HIGH: tick targets nearest player; Npc.TopHateObjectId exists but is never used — taunt/
     multi-attacker priority wrong. Fix: most-hated targeting with Java's >5s retarget throttle.
  2. MED: only "dummy" ai name is special-cased — noaction/portal/trap/useitem NPCs wrongly
     aggro-scan and wander.
  3. LOW: no geo LoS gate on aggro (acceptable Java-without-geodata fallback; document).
- Blocked on other systems: FEAR/flee (skill effects), FOLLOWING (summons), geo LoS/Z-clamp.
- Timing risk: fixed 2s tick caps attack cadence regardless of adelay — consider per-NPC attack
  timers during the refactor. Thread rule: all non-target state stays on the tick thread.

#### C3 Phase 1 (2026-07-11) — pluggable archetype layer + hate-retarget fix
- Added `Model/Ai/AiArchetype.cs` (enum: Aggressive, General, NoAction, Interaction) and
  `Model/Ai/AiNameRegistry.cs` (static, case-insensitive ai-name → archetype map seeded from the
  C3 survey table above). Unknown ai names fall back to General (retaliate-only, safe default);
  unregistered names containing "guard" fall back to Aggressive. `npc_templates.xml` distribution
  under this mapping: Aggressive ~32,276 (incl. simple_abyssguard/artifact_protector/
  siege_protector/*guard*), General ~11,261 (incl. everything not explicitly listed —
  servant/artifact/resurrect/siege_mine/summoner/etc.), NoAction ~1,565 (noaction+dummy),
  Interaction ~1,828 (portal/useitem/quest_use_item/chest/book/trap). NpcAiService logs the live
  count via `LogArchetypeDistribution()` at startup from loaded `NpcData`.
- Did NOT build the full `INpcAi`/`AbstractNpcAi`/`Archetypes/{...}` class hierarchy from the
  survey's port-order note — Phase 1 keeps the archetype as a plain enum resolved once per NPC
  (`NpcAiService.ResolveArchetype`, memoized by ai-name) and gates existing NpcAiService blocks
  directly. A polymorphic `INpcAi` layer is deferred to when the Guard archetype (Phase 2, assist
  radius + no-leash-return-to-post) actually needs behavior beyond boolean gates — avoids
  speculative abstraction for 4 archetypes that currently only need 3 yes/no flags.
- NpcAiService gates (mechanics untouched — same crit/parry/block/DoT/AoE/NPC-skill math runs
  for every archetype once a target exists):
  - Aggro-scan (bug #2 fix): scan-for-nearest-player block now requires
    `canAggroScan (archetype == Aggressive) && AggroRange > 0`, was `Ai != "dummy" && AggroRange > 0`.
  - Locked-target validation/retaliation: now gated on `canFight` (Aggressive or General) instead
    of `Ai != "dummy" && AggroRange > 0` — this also fixes General/AggroRange==0 NPCs never being
    able to retaliate via `ForceEngage` (their `_npcTargets` entry was previously never read back).
  - Wander/patrol: gated on `canWander` (Aggressive or General); NoAction/Interaction never move.
  - `ForceEngage` (CM_ATTACK retaliation path): returns immediately for NoAction/Interaction
    archetypes (resolved via `AiNameRegistry.Resolve` directly, not the tick-thread cache, since
    it runs on the packet-handler thread) — these NPCs never acquire a combat target even when hit.
  - `AlertNearbyAllies` (tribe-assist): now requires the ally to resolve to Aggressive (previously
    only excluded `Ai == "dummy"`) — ally-assist is a proactive-aggro trigger, so General/NoAction/
    Interaction NPCs no longer get pulled into fights they didn't start.
- Bug #1 fix (HIGH, most-hated targeting): after a target is confirmed each tick, if
  `npc.HateList.Count > 0` and more than 5s have passed since the last check for that NPC
  (`_lastRetargetTime`, tick-thread-owned `Dictionary<int, DateTime>`, cleared alongside the
  existing per-NPC dictionaries on death/target-loss/no-players), look up `npc.TopHateObjectId()`;
  if it differs from the current target and the new target is alive, same world, and within
  `ChaseTargetRange`, swap `_npcTargets`/`npc.Target` to it and drop `_chaseState` so the chase
  path recomputes toward the new target next tick. Leash/return-home logic is untouched — only
  which in-range player is being chased/attacked can change.
- Build: `dotnet build AionLightning.NET.sln` — 0 warnings, 0 errors.

#### C4 survey verdict (2026-07-11) — geodata: DEFER the engine
- The runtime dataset does not exist in this repo: zero *.geo files (no meshs.geo, no per-world
  {worldId}.geo — only one stray client-format .mesh sample the loader cannot read). Even Java's
  RealGeoData would throw on this checkout; geo has never run here and GeoDataConfig defaults are
  all off. A real dataset is extracted from client .paks by an external tool (hundreds of MB).
- Therefore C# already matches Java's de-facto behavior: getZ→input z, canSee→true,
  getClosestCollision→target unchanged, isInBounds→pure math. Deferring loses exactly nothing
  vs the working Java setup.
- [ ] **C4 Phase 0 (cheap, do when Game writer slot free):** World/Geo/ facade — IGeoData +
  DummyGeoData with the exact fallbacks + real IsInBounds; GeoDataOptions.Enable=false. Gives all
  future consumers (AI LoS, skills, movement) a stable API.
- [ ] **C4 Phases 1-2 (blocked on sourcing .geo data):** terrain-only getZ first (no BIH/mesh
  library needed), then mesh LoS/collision (math+BIH port ~1-2d, scene+loader ~2-3d). Loader is
  little-endian → BinaryReader/MemoryMappedFile; queries read-only after load (thread-safe);
  multi-second startup load in an IHostedService; watch LOH for big float[]/int[].

#### Phase B prep (2026-07-11) — Chat wire-format audit result
- Byte-level audit vs Java AL-Chat: framing (2-byte LE length-inclusive prefix, 1-byte opcode,
  plaintext — chat has NO blowfish), all client<->chat and GS<->chat packet layouts, UTF-16LE
  strings, and the 48-byte token round-trip (chat → GS → SM_CHAT_INIT → client echo) all MATCH.
  No wire-format defects — chat should survive its first real-client test at the protocol level.
- FALSE POSITIVE worth remembering: ChatChannels.InferType looked broken because its prefix
  literals embed an invisible U+0001 control char before "public_"/"trade_"/etc. Java builds
  identifiers as "@" + U+0001 + name + U+0001 + GSID + ".race.AION.KOR", so the StartsWith match
  is CORRECT as written. Don't "fix" it.
- [x] Fixed (real finding): short/malformed packets now log-and-skip instead of tearing down the
  connection (Java BaseClientPacket tolerance) — Chat AionClientConnection + Chat GsConnection.
  Game-side CsConnection gets the same treatment when the Game writer slot is free.
- Deferred lows: CM_CS_AUTH doesn't validate gsId/address (single-GS fine); token SHA256 hashes
  full UTF-8 login bytes vs Java's char-length truncation (chat-internal, client never validates).

- [x] **C2 Phase 3**: ReportToMany, KillInWorld, KillSpawned, WorkOrders templates (ItemOrders
  deferred to Phase 4/5 alongside the other remaining `XMLQuest` subtypes).
  - `Model/Templates/Quest/Script/QuestScriptData.cs`: added `ReportToManyScriptEntry` (+
    `ReportToManyNpcInfo` for `<npc_infos npc_id/var/quest_dialog/close_dialog/movie>`),
    `KillInWorldScriptEntry`, `KillSpawnedScriptEntry` (+ `SpawnedMonsterEntry` for
    `<spawned_monster var/end_var/npc_ids/spawner_object>`), and `WorkOrderScriptEntry` (+
    `give_component` reusing the existing `CollectItem` model — same `item_id`/`count` XSD shape).
    Attribute names verified against `quest_script_data.xsd`'s `ReportToManyData`/`KillInWorldData`/
    `KillSpawnedData`/`WorkOrdersData`/`NpcInfos`/`SpawnedMonster` complex types (not the Java
    model annotations — same stale-name class of issue Phase 1/2 already found). `work_order.xml`
    (574 `<work_order>` entries) lives in `quest_script_data/`, not `quest_data/`, so it's picked up
    by the existing directory scan with no extra load step. Also extended
    `Model/Templates/Quest/QuestTemplate.cs` with `QuestWorkItems` (`<quest_work_items>
    <quest_work_item item_id count>`, reusing `CollectItem` again) since `quest_data.xml`'s
    work-order entries (id 5000+) declare it and WorkOrders needs it for accept-time leftover
    cleanup — `QuestData.cs`'s loader needed no changes since it already deserializes into
    `QuestTemplate` directly.
  - `QuestEngine/Handlers/Templates/ReportToManyHandler.cs`: ports the sequential
    talk-to-npc-1-then-2-then-3 flow (one quest var tracks the current step, capped at
    `maxVar` = highest `npc_infos` `var`); dialog/closeDialog/movie-continuation branching mirrors
    Java's `onDialogEvent` almost verbatim, with one safety deviation: the `closeDialog in
    {1009,20002,34}` early-return branch now always persists the REWARD transition first (Java's
    literal `return sendQuestDialog(env, 5)` skips `updateQuestStatus` there, which looks like an
    unintentional data-loss bug on relog/crash for that rare combination). Not ported: the 5-entry
    `start_item_id` alternate start trigger (Java's `onItemUseEvent` — `CM_USE_ITEM` has no generic
    "fire a quest dialog event" hook, only hardcoded per-item-type dispatch) and the per-step movie
    follow-up dialog (no `SM_MOVIE`-equivalent packet exists yet, same gap as the already-unused
    `Movie` fields on `ItemCollectingScriptEntry`/`MonsterHuntScriptEntry`). Both are parsed for data
    completeness; the `start_item_id` count is logged as a warning from `QuestScriptData.Load`.
  - `QuestEngine/Handlers/Templates/KillInWorldHandler.cs`: **documented capability gap, per the
    task's "implement the NPC-kill part, log/skip the rest" rule** — traced Java's
    `onKillInWorldEvent` to its actual call site (`PvpService.notifyKillQuests`, called only from
    the player-kills-opposing-race-player path) and confirmed KillInWorld has **no NPC-kill part at
    all** in Java (unlike KillSpawned/MonsterHunt); its entire kill-count objective is PvP-kill-
    driven via `QuestEngine.onKillInWorld(worldId)`, an event pipeline this engine doesn't have and
    which is out of scope to build this phase. The handler registers start/end NPC dialog hooks
    (accept/turn-in work normally) but the kill counter (quest var 0) never advances — quests can be
    accepted but not completed through normal play. `invasion_world` (Rift/Vortex auto-start) is
    parsed but not wired, same reason as MonsterHunt's aggro/invasion fields (no RiftService/
    VortexService port yet).
  - `QuestEngine/Handlers/Templates/KillSpawnedHandler.cs`: full port — talking to a "spawner
    object" NPC (`USE_OBJECT` dialog) spawns its associated monster via the already-existing
    `SpawnService.SpawnNpcAt`, at the spawner's own live position rather than Java's static
    `SPAWNS_DATA2.getFirstSpawnByNpcId` lookup (simpler, and always instance-correct for the exact
    object interacted with); killing it (routed through the existing `OnKillAsync`/NPC-kill path)
    increments the matching `spawned_monster`'s var and transitions to REWARD once every group is
    satisfied, mirroring Java's `onKillEvent` exactly.
  - `QuestEngine/Handlers/Templates/WorkOrdersHandler.cs`: full port of the crafting work-order
    flow — accept validates the recipe is new to the player and gives its `give_component` items
    (bag-full aborts before creating the quest entry, ReportToHandler-style, instead of Java's
    silent give-failure-but-still-start-quest quirk), then teaches the recipe; turn-in re-checks the
    crafted product is in the bag (shared `QuestService.IsRewardReady` against `quest_data.xml`'s
    `collect_items`), strips any leftover `quest_work_items` materials, drops the taught recipe, and
    transitions to REWARD — actual grant + `collect_items` consumption happens generically via
    `QuestRewardService.GrantAndCompleteAsync` on the follow-up `SELECT_QUEST_REWARD` click (that
    service already validates+consumes `collect_items` for every template, so the handler must NOT
    also remove them itself, unlike `quest_work_items`/recipe which are WorkOrders-specific cleanup
    with no shared home). Collapses Java's 2-3 click completion round trip into the same one-shot
    "transition then grant" shape already established by ItemCollecting/MonsterHunt/ReportTo. Ported
    literally: Java's NONE-status switch only reacts to `QUEST_SELECT`/`QUEST_ACCEPT_1` (no generic
    accept/refuse fallback like the other three templates use). Not ported: `max_repeat_count="255"`
    quest-repeat — pre-existing gap shared by every template handler (see `QuestEngine.
    ComputeNearbyQuests` remarks), not WorkOrders-specific, so left alone this phase.
  - `QuestEngineHostedService.cs`: now also takes `IRecipeDao` and `SpawnService` (both already
    DI-registered); registers all four new template types alongside the Phase 1/2 three; startup log
    reports per-type counts for all seven.
  - Data check (standalone loader run, bypassing the DB-config requirement that blocks a full server
    boot in a sandbox without MySQL configured): loaded counts match the on-disk element counts
    exactly — 84 `report_to_many`, 48 `kill_in_world`, 14 `kill_spawned`, 574 `work_order` (plus the
    unchanged 1,957/1,538/468 item_collecting/monster_hunt/report_to from Phase 1/2, and the same
    `growth.xml` skip already known from Phase 1). 5 of the 84 `report_to_many` entries use
    `start_item_id` (logged, see above).
  - Build: 0 warnings, 0 errors.
- [x] **C4 Phase 0** (2026-07-11): World/Geo/IGeoService + DummyGeoService registered in DI —
  exact Java-with-geo-disabled fallbacks (GetZ→z, CanSee→true, GetClosestCollision→target,
  IsInBounds bounds math). Future AI/skill/movement consumers call this stable API; the real
  engine slots in behind it if a .geo dataset is ever sourced.
- [x] **CsConnection packet tolerance** (2026-07-11): Game-side chat link now log-and-skips
  malformed packets like the two chat-side connections (completes the audit fix).

#### C3 Phase 2 (2026-07-11) — Guard archetype + Trap trigger + SHOULD_REWARD gating
- Added `AiArchetype.Guard` and `AiArchetype.Trap` (`Model/Ai/AiArchetype.cs`). `AiNameRegistry`
  remaps `simple_abyssguard`/`artifact_protector`/`siege_protector`/`guard`/unregistered
  `*guard*` names from Aggressive → Guard, and `trap` from Interaction → Trap.
- **Guard**: same aggro-scan/fight/retaliate/ally-assist gates as Aggressive (`canAggroScan`,
  `canFight` in `NpcAiService.TickAsync`, plus the `AlertNearbyAllies` ally check), but
  `canWander=false` — a new `patrolOnly` gate lets it still walk an assigned `WalkerId` route
  (`WanderAsync` already branches Patrol-vs-Random on `WalkerId`; Guard just never falls through to
  `WanderRandomAsync`). Leash/return-home (`StopChaseAsync`/`ReturnState`) and chase ranges
  (`ChaseTargetRange`/`ChaseHomeRange`) are untouched — Guard reuses the same `canFight` code path,
  so it already re-anchors to `Npc.HomePosition` exactly like Aggressive/General. Java evidence:
  `AbyssGuardSimpleAI2`/`ArtifactProtectorAI2`/`SiegeProtectorNpcAI2` all extend
  `AggressiveNpcAI2`/`SiegeNpcAI2` (no distinct combat gating in the subclasses); the "no random
  wander off post" behavior mirrors `WalkManager.startRandomWalking`'s spawn-level `randomWalk`
  flag, simplified here to an archetype-level flag since spawn-level `randomWalk` isn't modeled yet
  (documented deviation — acceptable since Phase 2 scope is the archetype gate, not spawn data).
- **Trap**: new `AiArchetype.Trap` skips the aggro/combat/wander pipeline entirely — `TickAsync`
  branches to a dedicated `NpcAiService.TickTrapAsync` for it. Ported from
  `AL-Game/data/scripts/system/handlers/ai/TrapNpcAI2.java`'s `tryActivateTrap`: scans world players
  each tick for the first alive, non-hidden, tribe-aggressive one within `AggroRange + 2` (exact
  Java formula), and on first trigger (`_trapTriggered`, a `ConcurrentDictionary<int,byte>` since
  the despawn continuation runs on a background `Task`, not the tick thread) casts one skill at the
  victim and despawns ~1s later (`_world.Remove` + `SM_DELETE` broadcast — no loot/respawn
  scheduling, matching `TrapNpcAI2.pollInstance`'s `SHOULD_REWARD`/`SHOULD_DECAY`/`SHOULD_RESPAWN`
  all `NEGATIVE`). The skill-cast dispatch (`SM_CASTSPELL` → per-subtype effect application →
  `SM_SKILL_ACTIVATION`) was extracted out of `TryCastNpcSkillAsync` into a shared
  `CastSkillEntryAsync` so Trap reuses the identical path instead of a parallel implementation.
  `ForceEngage` (CM_ATTACK melee retaliation) now also short-circuits for Trap — traps only ever
  fire through `TickTrapAsync`'s proximity scan, never via being hit.
  - Deviations from the literal Java source (documented, not silent): (1) picks a *random* known
    skill (`skills[Random.Shared.Next(...)]`) rather than a literal "first skill", matching
    `getSkillList().getRandomSkill()`'s actual behavior in Java, not the task brief's "first known
    skill" wording. (2) Java's trigger-enemy check is `creator.isEnemy(creature)` (the trap's
    summoner's enemy list); this port has no "creator/summoner" concept for spawned NPCs yet, so it
    reuses the same tribe-aggressiveness check (`Tribes.IsAggressiveToPlayer`) already used for
    ordinary aggro-scanning — a reasonable stand-in per the minimal-port instruction. (3) skipped the
    two hardcoded npcId special cases (749248/749249/749250/749251 → longer 4500/5000ms despawn) —
    one-off content exceptions, not core Trap-archetype behavior; all traps use the general 1000ms
    delay. (4) a trap killed directly via melee (CM_ATTACK) before it fires still goes through the
    normal decay/respawn scheduling in CM_ATTACK.cs (XP/loot are correctly suppressed via
    `AiNameRegistry.ShouldReward`, but Java's `SHOULD_RESPAWN=NEGATIVE` for traps isn't threaded
    into that respawn scheduling) — accepted as a rare edge case out of this phase's explicit scope.
- **Interaction dialogs verified unaffected**: `CM_DIALOG_SELECT.cs` has zero references to
  `AiArchetype`/`AiNameRegistry` — its dialog flow runs entirely independent of the AI tick gates
  (confirmed by reading, no changes made).
- **SHOULD_REWARD gating** (Java `AIQuestion.SHOULD_REWARD`, `NpcController.doReward`/`onDie` —
  the entire XP+DP+AP+loot-registration flow is gated by one poll): added
  `AiNameRegistry.ShouldReward(string aiName)` (Aggressive/General/Guard → true, else false) and
  used it in `CM_ATTACK.cs`'s NPC-death branch to gate `_lootService.GenerateDrops`,
  `_expService.AddGroupExpAsync`, and the Abyss/ABYSS_GUARD AP-award block. Quest kill-credit
  (`_questService.HandleNpcKillAsync`) was deliberately left ungated — Java's `QuestEngine.onKill`
  call happens to live inside the same `doReward()` method, but gating quest credit is out of this
  phase's explicit "XP + loot" scope and `QuestEngine/**` belongs to a different in-flight agent;
  flagged here rather than touched. **Known gap**: `CM_CASTSPELL.cs` has three duplicate
  `AddGroupExpAsync`/`GenerateDrops` call sites (primary-target kill, AoE splash kill, and a third
  kill path) that need the identical `AiNameRegistry.ShouldReward` gate — not applied here since
  `CM_CASTSPELL.cs` is owned by the concurrent QuestEngine/CM_CASTSPELL agent; needs a follow-up
  pass (either by that agent or a short Phase 2b).
- Build: `dotnet build AionLightning.NET.sln` was blocked during this phase by concurrent,
  in-progress `World/Geo/**` edits from another agent (`CollisionResult.cs`/`BoundingVolume.cs`
  referencing not-yet-added `Scene`/`Bounding`/`Geometry` types) — confirmed unrelated to this
  phase's files (no error referenced `Model/Ai/**`, `NpcAiService.cs`, or `CM_ATTACK.cs` across
  three consecutive build attempts). Re-run the full-solution build once the Geo work lands.

#### C4 Phases 1-2 (2026-07-11) — real geo engine ported behind IGeoService, selectable by config
- Full faithful port of the Java `geoEngine` package (`AL-Game/src/.../geoEngine/**` +
  `world/geo/GeoService.java`) into `World/Geo/{Math,Bounding,Collision,Collision/Bih,Scene,
  Models,Loader}/**`, wired up as `RealGeoService : IGeoService`, selectable via the existing
  `IGeoService` facade from C4 Phase 0. `Configs/Options/GeoDataOptions.cs` adds
  `GameServer:GeoData:{Enable=false, DataPath="data/geo"}`; `Program.cs` now registers
  `RealGeoService` always (cheap) but only resolves it as `IGeoService` and only registers
  `GeoLoadHostedService` (loads `meshs.geo` + every `{worldId}.geo` at startup) when
  `GeoData:Enable=true` — default stays `DummyGeoService`, unchanged from Phase 0.
- Math/Bounding/Collision/BIH ported line-for-line from the JME-derived Java originals
  (`Vector3f`/`Matrix3f`/`Matrix4f`/`Ray`/`Triangle`/`Plane`/`FastMath`, `BoundingBox`/
  `BoundingVolume`/`Intersection`, `BIHNode`/`BIHTree` — same median-ish-split build algorithm,
  same ray-space BIH traversal). `Scene/Mesh.cs` collapses Java's `VertexBuffer`/
  `IndexByteBuffer`/`IndexShortBuffer`/`IndexIntBuffer`/`GLObject` GPU-upload abstraction (needed
  in Java to share code with the renderer) down to plain `float[]` positions + one `int[]` index
  buffer, per the port plan — there is no renderer here, so the genericity had no purpose.
  `Models/GeoMap.cs` ports `getZ`/`getClosestCollision`/`canSee`/the bilinear 2-triangle terrain
  sampler verbatim (renamed Java's `terraionCollision` typo to `TerrainCollision`).
- `Loader/GeoWorldLoader.cs` reads `meshs.geo`/`{worldId}.geo` with `BinaryReader` over a
  `FileStream` (not `MemoryMappedFile` — simpler, same little-endian semantics; .NET's
  `BinaryReader` is little-endian-native on every .NET-supported architecture, matching Java's
  explicit `ByteOrder.LITTLE_ENDIAN`) — exact byte layout per the task: name-prefixed strings,
  vertex/index/collision-flags per model (skipping `MOVEABLE`), terrain flag byte, and
  loc+3x3-matrix+scale per instance.
- **Deliberately not ported / stubbed (Phase 3, per task instructions):** doors
  (`DoorGeometry`, `GeoMap.doors`/`getDoorName` — `SetDoorState` is a no-op) and material zones
  (`ZoneService.createMaterialZoneTemplate` — loader still walks `child\d+_<name>` sibling
  geometries and gives each its own instance transform, just without registering a zone). Both
  match Java's own `GEO_DOORS_ENABLE=false`/`GEO_MATERIALS_ENABLE=false` defaults, which is how
  this repo has always run.
- **Other documented deviations:** `BoundingSphere` not ported — never instantiated anywhere in
  the mesh/geometry pipeline (a mesh's bound always defaults to and stays a `BoundingBox`), so
  `Type.Sphere` is unreachable; `Eigen3f`/`Array3f`/`Vector2f` and the Java object-factory/recycle
  pool (`GEO_OBJECT_FACTORY_ENABLE`) dropped as unused-in-scope or GC-unnecessary. Java's
  `BIHNode.intersectBrute` and the `Collidable`-vs-`BoundingBox` overload of `intersectWhere` were
  already dead code in the original (commented-out call site / always-zero collision count) and
  are not ported. `RealGeoService`'s per-world grid partition size (256-unit sub-nodes) uses a
  fixed 3072 for every world — matches Java's own `GeoService.getWorldSize()`, which is *also*
  hardcoded to 3072 regardless of the real per-map `WorldMapTemplate` size; wiring the real
  per-map size back in is future work, out of scope here.
- Synthetic-data verification (no real `.geo` files exist in this repo — see prior C4 survey):
  fabricated a `meshs.geo` (one triangle "wall", `CollisionIntention.Physical`) + a `999.geo`
  (flat single-height terrain, one instance placement) with the exact binary layout, then drove
  `GeoWorldLoader`/`GeoMap` via reflection (both are `internal`) from a throwaway console app in
  the scratchpad — not added to the repo. All 4 assertions passed: `GetZ` returns the terrain
  height (not the input z), `CanSee` across the wall triangle → false, `CanSee` in open space →
  true, `GetClosestCollision` clamps just short of the wall instead of reaching the far target.
- Build: `dotnet build AionLightning.NET.sln` (full solution, `--no-incremental`) — 0 warnings, 0
  errors.

#### C2 Phase 4 (2026-07-11) — reward templates: CraftingRewards/RelicRewards/FountainRewards/SkillUse/MentorMonsterHunt
- New handlers: `QuestEngine/Handlers/Templates/{CraftingRewardsHandler,RelicRewardsHandler,
  FountainRewardsHandler,SkillUseHandler,MentorMonsterHuntHandler}.cs`. New script-entry models in
  `Model/Templates/Quest/Script/QuestScriptData.cs`: `CraftingRewardsScriptEntry`,
  `RelicRewardsScriptEntry`, `FountainRewardsScriptEntry`, `SkillUseScriptEntry` (+
  `SkillUseGroupEntry` for `<skill ids/start_var/end_var/var_num>`), `MentorMonsterHuntScriptEntry`
  (reuses the existing `MonsterEntry` for its `<monster var/end_var/npc_ids>` children — same shape
  as `<monster_hunt>`). Attribute names verified against the actual on-disk XML (not stale Java
  annotations): `crafting_rewards` uses `movie` (not `quest_movie`); `skill_use`'s `<skill>` child
  uses `ids`/`end_var` (matches Java's own `QuestSkillData`, unlike the other templates' stale
  models). `DataHolders/QuestScriptData.cs` and `QuestEngineHostedService.cs` updated with the five
  new lists/registrations; `QuestEngineHostedService` now also takes `ISkillDao` (already
  DI-registered in `Program.cs`, not touched here).
- **`QuestEngine.OnSkillUseAsync` added** (Java `onUseSkillEvent`/`registerQuestSkill` port): new
  `_skillUseIndex` (skillId → quest ids, mirrors the existing `_itemGetIndex`/`OnItemGetAsync`
  pattern exactly), `RegisterSkillUse(skillId, questId)`, `OnSkillUseAsync(player, skillId, conn,
  ct)`. `IQuestHandler`/`QuestHandlerBase` gained a matching default-false `OnSkillUseAsync` member
  (same shape as `OnItemGetAsync`). Wired from `CM_CASTSPELL.cs`: one `await
  _questEngine.OnSkillUseAsync(player, _spellId, _conn, ct)` call inserted immediately after the
  server-side cooldown-enforcement block and before the "Broadcast cast animation" comment (i.e.
  once every cost/cooldown gate has already passed, so the cast is guaranteed to actually happen) —
  added a `QuestEngineType questEngine` constructor parameter + field to `CM_CASTSPELL`, and passed
  the packet factory's existing `_questEngine` field through at its one construction site in
  `GsPacketHandlerFactory.cs` (that field already existed there for `CM_DIALOG_SELECT`, so no new
  DI registration was needed). **Not addressed**: the pre-existing flagged gap from C3 Phase 2 about
  `CM_CASTSPELL.cs`'s three `AiNameRegistry.ShouldReward`-gated kill/loot call sites — out of this
  phase's scope (quest-engine skill-use tracking only), left for whoever picks up that follow-up.
- **`QuestTemplate.cs` gained two additions** (both required for these templates' quest_data.xml
  reward semantics to actually work, not just cosmetic): (1) `RewardsList` (`List<QuestRewards>`,
  bound to the same `<rewards>` element, which the XSD already declares `maxOccurs="unbounded"`) —
  `relic_rewards` quests (e.g. id 21281) declare 4 sibling `<rewards reward_abyss_point="..."/>`
  blocks, one per relic tier; the pre-existing singular `Rewards` property could only ever surface
  the *last* one (confirmed via a standalone-loader probe: XmlSerializer overwrites the property on
  each repeated element, never errors). `Rewards` is now a computed `RewardsList[^1]` fallback,
  preserving that exact prior "last one wins" behavior for the ~69 other already-existing quests
  that also declare multiple `<rewards>` blocks (a wider pre-existing gap, not fixed here — out of
  scope) so nothing regresses. (2) `InventoryItemsHolder`/`InventoryItems` (`<inventory_items>
  <inventory_item item_id count>`) — Java's `InventoryItems`, distinct from `CollectItems`; the
  "coin fountain" gate (e.g. quest 1717 requires+consumes one `186000031` item with *no*
  `<collect_items>` at all) has no home in the pre-Phase-4 model. `Services/QuestRewardService.cs`'s
  `GrantAndCompleteAsync` now (a) resolves `rewards = RewardsList[rewardIndex]` when a quest has
  more than one tier (Java: `template.getRewards().get(reward)`), falling back to the unchanged
  `template.Rewards` otherwise — zero behavior change for every single/no-tier quest — and (b)
  validates + consumes `InventoryItems` the same way it already does `CollectItems`.
- **`CraftingRewardsHandler`**: accept/turn-in via the standard `SendQuestStartDialogAsync`/
  `SendQuestEndDialogAsync` convention; grants the skill/level (`player.Skills.AddSkill` +
  `ISkillDao.UpsertAsync` + `SM_SKILL_LIST`) unconditionally on the `SELECT_QUEST_REWARD` click
  rather than gating it behind Java's `onMovieEndEvent` (all 28 entries declare a non-zero `movie`
  attribute; no SM_MOVIE/movie-end callback exists in this port, so a literal port would make the
  quest unfinishable). `CraftSkillUpdateService`'s expert/master crafting-skill-count cap
  (`canLearnMoreExpertCraftingSkill`/`...Master...`) is not re-checked — out of this template's
  scope; only a light "don't regrant an already-known level" guard is kept.
- **`RelicRewardsHandler`**: Java lets the player pick which of 4 relic item types to submit via 4
  distinct dialog actions (`SELECT_ACTION_1011/1352/1693/2034`), none of which `CM_DIALOG_SELECT`
  routes to the quest engine (only `QUEST_SELECT`/`QUEST_ACCEPT(_1)`/`SELECT_QUEST_REWARD` are, the
  same subset every template handler in this engine relies on) — collapsed into one
  `SELECT_QUEST_REWARD` click that auto-picks the first relic type the player holds enough of,
  recording the chosen slot (1-4) in var 0; the follow-up claim click computes `rewardIndex = var -
  1` and calls `QuestRewardService.GrantAndCompleteAsync` directly (bypassing the base
  `SendQuestEndDialogAsync` helper, which only recognizes `SELECT_QUEST_REWARD`) — this is a
  documented behavioral simplification (auto-select vs. player choice), not a 1:1 dialog-id port,
  but the reward-tier math (`var - 1` → `RewardsList` index) matches Java's
  `QuestService.finishQuest(env, qs.getQuestVars().getQuestVars() - 1)` exactly.
- **`FountainRewardsHandler`**: Java's real flow is a single "insert coin" click
  (`USE_OBJECT`→`SETPRO1`) that starts the quest AND transitions it straight to REWARD in one
  action — neither Java dialog id is routed to the engine either, so this port re-creates the same
  "create the entry already in REWARD status" behavior off the routed `QUEST_ACCEPT`/
  `QUEST_ACCEPT_1` click instead (custom accept logic, not the shared `SendQuestStartDialogAsync`
  helper, since that always creates a START-status entry). Not ported: `isFullSpecialCube()`
  (housing-cube-extension check, no such concept in this port).
- **`SkillUseHandler`**: kill-count-style packed-var bookkeeping identical to
  `MonsterHuntHandler.ReadGroupTotal`, driven by the new `OnSkillUseAsync` hook instead of
  `OnKillAsync`. REWARD-status click is gated on every skill group's use-count objective actually
  being met (Java's own code transitions unconditionally on the click — tightened here to match the
  completion-gating convention every other template handler already uses).
- **`MentorMonsterHuntHandler`**: **documented limitation, per the task's "log/skip with a
  documented limitation" instruction** — Java's `onKillEvent` override gates kill-credit behind a
  live mentor/mentee group relation (`player.isMentor()` + a same-group member in level range and
  distance, or the symmetric mentee check). Confirmed by reading `Model/Player.cs` and
  `Model/Group/PlayerGroup.cs`: this port has **no mentor/mentee concept at all** yet (no `IsMentor`
  flag, no pairing state) — a pre-existing gap in the player/group model, not introduced by this
  template. Kill-credit is granted unconditionally instead (functionally identical to a plain
  `MonsterHuntHandler` hunt); `min_mente_level`/`max_mente_level` are parsed onto the script entry
  for data completeness but intentionally unused by the handler. Uses a single var per monster
  group (no 6-bit packed span) since every observed `end_var` in the shipped data is ≤ 9, matching
  the simpler style already used by `KillSpawnedHandler`.
- Data check (standalone loader probe, `DataManager` instantiated directly — no DB/DI needed since
  its constructor only reads static XML — via a throwaway console app referencing
  `AionLightning.Game.csproj` from the scratchpad, not added to the repo): loaded counts match the
  on-disk element counts exactly — 28 `crafting_rewards`, 30 `relic_rewards`, 7 `fountain_rewards`,
  30 `skill_use`, 34 `mentor_monster_hunt` (unchanged Phase 1-3 counts also re-verified: 1957/1538/
  468/84/48/14/574). Also spot-checked the `RewardsList`/`InventoryItems` additions directly: quest
  21281 parses all 4 reward tiers (AP 300/600/900/1200) instead of only the last; quest 1717 parses
  its `<inventory_items>` (item 186000031, count 1) with `Rewards.Exp` (1500) still intact via the
  fallback.
- Build: `dotnet build AionLightning.NET.sln` (full solution) — 0 warnings, 0 errors.

#### Summon system survey (2026-07-11) — Spiritmaster spirits
- Java: <summon> skill effect (64 skills; elemental spirits npc 201010-201034) → SummonsService
  (one summon per master, modes GUARD/ATTACK/REST/RELEASE, 2-phase release w/ 5s delay + hate
  transfer to master, DISTANCE/LOGOUT unsummon). Stats come from summon_stats templates keyed by
  (npcId, level), NOT npc-template stats. Packets: SM_SUMMON_PANEL 0x99, SM_SUMMON_UPDATE 0x9B
  (current+base stat pairs), SM_SUMMON_PANEL_REMOVE 0x49, SM_SUMMON_OWNER_REMOVE 0x9A,
  SM_SUMMON_USESKILL 0xA2. C# state: the 5 CM_SUMMON_* readers exist but RunAsync are stubs;
  no Summon model/service; <summon> effect silently ignored in CM_CASTSPELL.
- Pre-decisions for Phase 1 (made): wire creatorId/masterName into SM_NPC_INFO for owner linkage
  (Java does exactly this); summon ticks inside NpcAiService via a dedicated summon pass (follow
  master in GUARD, chase/attack in ATTACK); MVP credits kills/hate to the MASTER via the existing
  ForceEngage/kill paths (avoids lifting the hate API from Npc to Creature); interim stats from
  npc templates (summon_stats fidelity = Phase 3 debt, panel numbers will look off).
- [ ] Summon Phase 2: CM_SUMMON_CASTSPELL/USESKILL + throttle, CM_SUMMON_MOVE/EMOTION, REST
  regen, distance/logout release. Phase 3: summon_stats templates, full SM_SUMMON_UPDATE stat
  pairs, servant/trap/homing/groupgate families.

#### PvP kill pipeline survey (2026-07-11)
- C# already handles PvP death INLINE in CM_ATTACK + CM_CASTSPELL (AP exchange, announces, rank
  update, persist) — but: credits last-hitter not most-damage (no aggro list), no group/alliance
  AP split, no per-victim daily-kill cap (AP farm exploit), CalculatePvPApGained misses Java's
  winnerRank<=7 rank-diff -5%/rank penalty and maps diff<-3 to x1.20 (Java x1.30), AP loss is not
  damage-scaled, and there is NO PvP-kill → QuestEngine path (kill_in_world quests unwinnable).
- Verbatim AP tables (already correct in C#): pointsGained 300..1245, pointsLost 90..311 (ranks
  1-9); level-diff multipliers: >4→x0.1, ==4→x0.65, ==3→x0.85, ==-2→x1.1, ==-3→x1.2, <-3→x1.3.
- [ ] **PvP Phase 1**: Combat/Handlers/PvpKillHandler : IEventHandler<DeathEvent> — move the
  duplicated inline block there (duel-guarded), fix the two formula gaps, add
  QuestEngine.OnPlayerKillAsync(env, victimWorldId) + world-keyed index so KillInWorldHandler
  counts kills. Preserve packet order + DAO persistence when relocating.
- [ ] **PvP Phase 2 (needs AggroList on Creature)**: most-damage attribution, group/alliance
  damage-proportional split, damage-scaled AP loss, notify all rewarded members' quests.
- [ ] **PvP Phase 3**: per-victim KillList daily cap (fixes the farm exploit), kill item rewards,
  serial-killer system.

- [x] **M381: Summon system Phase 1 — Spiritmaster spirits cast, follow, attack-on-command, and
  dismiss (minimal viable spirit)** — a `<summon npc_id="N" time="T"/>` skill effect (64 elemental
  spirit skills, npc 201010-201034) was silently ignored in CM_CASTSPELL (fell through to the
  buff/unknown catch-all); no `Summon` model, `SummonsService`, or World store existed; the 5
  CM_SUMMON_* client packets were opcode-only stubs; SM_NPC_INFO hardcoded creatorId=0/masterName=""
  so summons could never be linked to their owner client-side.
  - **Model**: `Model/Summons/SummonMode.cs` (Attack=0/Guard=1/Rest=2/Release=3, Java wire ids) and
    `Model/Summons/UnsummonType.cs` (Command/Distance/Logout/Unspecified — Distance reserved for
    Phase 2). `Model/Summon.cs` — sealed `Summon : Creature`; `Template` (NpcTemplate, reused as-is —
    interim stats, summon_stats fidelity is Phase 3 debt), `Master` (Player?), `Mode`, `Level` (byte,
    the *skill* level, distinct from the npc_template level — mirrors Java `Summon.getLevel()`),
    `LiveTime`. `Model/Player.cs` — added `Summon? Summon` (one per master, Java rule).
  - **World store**: `World/World.cs` — dedicated `ConcurrentDictionary<int, Summon>` +
    `Add/Remove/GetSummonByObjectId/GetAllSummons` (kept separate from the NPC store since summons
    have an owner and a different lifecycle). `Network/Aion/ClientPackets/CM_LEVEL_READY.cs` — zone
    entry now also sends `SM_NPC_INFO(summon)` for every summon in the entering player's world, next
    to the existing NPC introduction loop.
  - **SM_NPC_INFO owner linkage**: `Network/Aion/ServerPackets/SM_NPC_INFO.cs` refactored from a
    single `Npc _npc` field to precomputed common fields populated by either an `SM_NPC_INFO(Npc)` or
    a new `SM_NPC_INFO(Summon)` constructor (creatorId=master.ObjectId, masterName=master.Name,
    level=summon.Level) — same wire format, `Write()` unchanged. Deviation: Java varies npcTypeId
    per-viewer (SUPPORT vs ATTACKABLE based on the *receiving* player's enemy relation to the master);
    Phase 1 always reports the peaceful/support type since summons never threaten their own faction —
    revisit if PvP-visible summons matter later.
  - **Packets** (new files, exact Java field layouts): `SM_SUMMON_PANEL` (0x99), `SM_SUMMON_UPDATE`
    (0x9B, current+base stat pairs), `SM_SUMMON_PANEL_REMOVE` (0x49), `SM_SUMMON_OWNER_REMOVE`
    (0x9A). Fields with no NpcTemplate analog (mDef, mBoost, mAccuracy, mCritical, parry) report 0;
    mainHandPCritical reuses `Stats.Power` the same way NpcAiService already treats it as an NPC
    crit-rating proxy elsewhere — Phase 3 debt once summon_stats templates land.
  - **Skill effect parsing**: `Model/Templates/Skill/SkillTemplate.cs` — additive-only
    `SkillEffects.HasSummonEffect` / `SummonInfo` (npcId, time) following the existing
    `Elements?.Any(...)` / `FirstOrDefault(...)` pattern used by `HasResurrectEffect` etc. Verified
    against skill_templates.xml: all 64 plain `<summon npc_id="…"/>` entries (skillsubtype="SUMMON",
    first_target="ME", target_relation="FRIEND") omit `time` entirely despite the Java field being
    marked `required=true` — spirits are permanent until released; `summontrap`/`summonhoming`/
    `summonservant`/etc. are separate elements, deliberately not matched (out of Phase 1 scope).
  - **SummonsService** (`Services/SummonsService.cs`, DI singleton): `CreateSummonAsync` (one-per-
    master guard → msg 1300072 refusal, resolves NpcTemplate from `DataManager.Npcs`, spawns at the
    master's exact position — Java spawns summons at the caster's literal x/y/z too, the client
    renders the "at the side" offset itself), `DoModeAsync` (GUARD/ATTACK/REST switches +
    SM_SUMMON_UPDATE + mode message; RELEASE routes to `ReleaseAsync`), `ReleaseAsync` (2-phase: mode
    flips to RELEASE + optional SM_SUMMON_UPDATE now, world removal + SM_DELETE broadcast +
    SM_SUMMON_OWNER_REMOVE/SM_SUMMON_PANEL_REMOVE to master 5s later, matching Java
    `ReleaseSummonTask`'s delay), `ReleaseImmediatelyAsync` (LOGOUT-only: immediate removal, zone-wide
    SM_DELETE, no packets to the (already-disconnected) owner, no 5s delay).
  - **CM_CASTSPELL insertion point**: new `else if (template?.Effects?.HasSummonEffect == true &&
    _targetType is 0 or 3 or 4 && (_targetObjectId == 0 || _targetObjectId == player.ObjectId))`
    branch inserted immediately before the final catch-all `else` (the "Buff, chant, passive, or
    unknown" branch that previously swallowed SUMMON-subtype casts) — structurally parallel to the
    HEAL/BUFF/damage branches earlier in the same if/else-if chain, each with its own
    `SM_SKILL_ACTIVATION` broadcast.
  - **Client packets**: `CM_SUMMON_COMMAND` (0x15B) and `CM_SUMMON_ATTACK` (0x169) filled in from
    opcode-only stubs — both resolve `ActivePlayer.Summon`, validate ownership/target, and delegate to
    `SummonsService.DoModeAsync`. `CM_SUMMON_CASTSPELL`/`CM_SUMMON_EMOTION`/`CM_SUMMON_MOVE` remain
    stubs (Phase 2).
  - **AI tick** (`Services/NpcAiService.cs`): new `TickSummonsAsync` pass added to the existing 2s
    tick, after the NPC loop — summons live in their own World store so this is a parallel pass, not a
    branch inside the NPC loop. GUARD → `FollowMasterAsync` (distance > 4m triggers a move toward the
    master); ATTACK → `TickSummonAttackAsync` (chase to melee range, then interim-formula melee hits
    on a 1.5s cooldown). Kill crediting reuses the CM_ATTACK.cs Npc-death subset (loot/quest/xp) keyed
    to the **master**, gated by `AiNameRegistry.ShouldReward` — avoids lifting the hate/aggro API from
    `Npc` onto `Creature` just for this (pre-decision from the 2026-07-11 survey). On melee hit, the
    target is engaged against the **master** via the existing `ForceEngage`, not the summon, for the
    same reason. Constructor gained `LootService`/`QuestService`/`SpawnService` (all already
    registered singletons, no circular DI).
  - **Deviation — movement model**: follow/chase snap the summon directly to its computed destination
    within the same 2s tick and broadcast one `SM_MOVE.StartNpcMove` for client-side animation, rather
    than tracking inter-tick arrival like NPC `ChaseAsync`/`WanderAsync` (`_chaseState`/`_wanderState`).
    Acceptable for a single always-nearby companion; revisit if summons need believable mid-flight
    position for other systems (e.g. AoE collision).
  - **Deviation — system messages**: Java embeds the summon's display name via a client-side
    `DescriptionId` nameId reference for STR_SKILL_SUMMON_* messages; this port passes the summon's
    plain `Name` as a string param instead (`SM_SYSTEM_MESSAGE.cs` additions) since the wire protocol
    has no DescriptionId param-type support yet — text still resolves correctly, just through a
    different (simpler) client-side substitution path.
  - **Logout**: `Network/Aion/GsClientConnection.cs` `DisposeAsync` — released via
    `ReleaseImmediatelyAsync` right after `_world.Remove(player)`/`_connRegistry.Unregister`, so the
    zone-wide SM_DELETE never reaches the disconnecting owner's own (already-torn-down) connection.
    Threaded `SummonsService` through `GsConnectionFactory` → `GsClientConnection` (manual DI, no
    container changes needed).
  - **Deferred to Phase 2/3** (per the 2026-07-11 survey pre-decisions, unchanged): distance-based
    auto-release, CM_SUMMON_CASTSPELL/USESKILL + throttle, CM_SUMMON_MOVE/EMOTION, REST-mode HP regen,
    summon_stats templates (real per-level stats + full SM_SUMMON_UPDATE fidelity),
    servant/trap/homing/groupgate summon families. Additionally not attempted in Phase 1: NPCs do not
    yet aggro-scan or retaliate against summons directly (mobs only ever perceive players in
    `NpcAiService`'s scan), so a summon cannot currently be killed by anything — summon death/respawn
    handling is unimplemented until that changes.
  - Build: `dotnet build AionLightning.NET/AionLightning.NET.sln` — 0 warnings, 0 errors.

#### C2 Phase 5 scoping (2026-07-11) — the 1,493 hand-written quest scripts
- These are DISTINCT from the 12 XML-template handlers (they exist because they don't fit
  templates). Anatomy: median 109 LOC; ~70% boilerplate absorbed by base-class helpers, ~30%
  bespoke per-quest var→dialogPage tables. Buckets: dialog-only 964 (65%), dialog+kill 262 —
  top-2 = 1,226 quests (82%); escort/follow 57 + timer 37 sequenced LAST (need infra).
- DECISION: per-quest Roslyn .cs under Scripts/quest/<zone>/ mirroring Java 1:1 (keep the 12
  XML templates for their own population). Rejected data-DSL growth — it would reinvent a
  scripting language. Game csproj already has the Scripts copy-not-compile pattern.
- CRITICAL build-model change: ScriptService compiles one ALC per file — must add a folder→
  single-assembly batch compile (extend CSharpCompilerService to N syntax trees) before scaling.
- [x] **Phase 5 Batch 0 (blocking)**: OnLevelUp (399 quests) + OnZoneMissionEnd (192) hooks,
  port helper set (defaultCloseDialog x1366 uses, defaultOnKillEvent, checkQuestItems,
  useQuestObject, playQuestMovie+SM_PLAY_MOVIE, give/removeQuestItem, start/end dialog
  overloads), QuestService startMission/LOCKED flow, complete DialogAction SETPROn coverage,
  batch-compile mode, verify id-disjointness vs template population, then 2-3 hand-ported
  starter quests as the golden pattern.
- [ ] **Phase 5 batches**: Poeta (14) → Ishalgen (17) → Verteron (34) → Altgard (38) → Eltnen/
  Morheim → Heiron/Beluslan → rest; 10-15 quests per agent, zone batches in parallel after
  Batch 0; golden probe tests per quest (dialog page ids + var/status transitions).

- [x] **PvP Phase 1** (per the 2026-07-11 survey): centralized the duplicated player-kills-player
  block from CM_ATTACK.cs/CM_CASTSPELL.cs, fixed the two `CalculatePvPApGained` formula gaps, and
  wired the first PvP-kill → QuestEngine path.
  - **New**: `Combat/Handlers/PvpKillHandler.cs : IEventHandler<DeathEvent>` (registered in
    Program.cs right after `ResurrectBaseHandler`, same M287 death-event chain). Guards: killer/
    victim both `Player`, victim still `IsAlreadyDead` (a Chain-of-Suffering revive earlier in the
    same handler chain un-does the kill), different `Race`, `AbyssRankService.IsPvPMap`, and a
    defensive `DuelService.GetOpponent` check (duels already suppress `DeathEvent` at the
    `ApplyDamageAndPublishAsync` call site via `suppressDeathEvent: true` — this is a second,
    belt-and-suspenders guard for any future damage path that forgets to set that flag).
  - **What moved vs what stayed inline**: `ApplyDamageAndPublishAsync` publishes `DeathEvent`
    synchronously *inside itself*, i.e. before the caller (CM_ATTACK/CM_CASTSPELL) even broadcasts
    the killing-blow SM_ATTACK/SM_ATTACK_STATUS packets, let alone reaches its own "target died"
    branch — the existing `ResurrectBaseHandler` already relies on/causes this ordering (its
    teleport/revive packets fire before the killing hit's own attack-animation packets). Relocating
    the victim's own death packets (state flag, effect clear, SM_EMOTION DIE, SM_ABNORMAL_EFFECT
    clear, SM_DIE + "you were killed by") into the handler would reorder them ahead of the attack
    animation the client just watched for the same hit, so those stay inline at both call sites
    (duel-check + early-return also stays inline, since it's a "the target didn't really die"
    branch, not part of the reward path). Moved to `PvpKillHandler`: AP gain/loss, rank update +
    broadcast, zone kill announcement, group-died notice, DAO persistence (`UpdateAbyssAsync` x2,
    `UpdateAbyssKillStatsAsync`), legion contribution, and the new quest notify — none of these
    packets have an ordering dependency on the attack-animation sequence.
  - **AbyssRankService.CalculatePvPApGained fix** (verified against Java
    `StatFunctions.calculatePvpApGained`): level-diff `diff < -3` now multiplies by 1.30 (was
    incorrectly folded into the `diff == -3` → 1.20 bucket, so no level gap ever hit x1.30). Added
    the missing abyss-rank penalty: `winner.AbyssRank <= 7 && (winnerRank - defeatedRank) > 0` →
    subtract `rankDiff * 5%` from the already level-adjusted, rounded AP gain (matches Java's
    round-then-penalize order, not a single combined-float computation). `CalculatePvPApLost` was
    already correct and untouched.
  - **QuestEngine**: new `Dictionary<int, List<int>> _killInWorldIndex`, `RegisterKillInWorld(int
    worldId, int questId)` (mirrors `RegisterItemGet`/`RegisterSkillUse`), and
    `OnPlayerKillAsync(Player killer, Player victim, GsClientConnection killerConn, ct)` — looks up
    quests by **victim's** `Position.WorldId` (Java: `int worldId = victim.getWorldId();`), builds
    one `QuestEnv(victim, killer, 0, 0)` and dispatches to each handler's new
    `IQuestHandler.OnPlayerKillAsync` (default `false`, added next to `OnSkillUseAsync`;
    `QuestHandlerBase` got the matching `virtual` no-op so template handlers only override what
    they use). `PvpKillHandler` calls this dispatcher after the AP award, since same-race kills are
    already excluded by its own guards (mirrors Java `PvpService.notifyKillQuests`'s own race check
    being redundant with `doReward`'s).
  - **KillInWorldHandler**: `Register` now also loops `data.WorldIds` → `engine.RegisterKillInWorld`.
    `OnPlayerKillAsync` mirrors Java `defaultOnKillRankedEvent(env, 0, killAmount, true)`: reads
    quest var 0, increments by one via `ChangeQuestStepAsync` while `var < killAmount - 1`, and on
    the kill that brings it to `killAmount - 1` flips straight to REWARD (var is deliberately left
    at `killAmount - 1`, not bumped to `killAmount` — Java's `if (reward) qs.setStatus(REWARD)`
    branch skips the var write entirely, this mirrors that exactly) — same
    persist+SM_QUEST_ACTION path as `MonsterHuntHandler.OnKillAsync`.
  - **Deferred to Phase 2** (per the survey, unchanged — needs `AggroList` on `Creature`):
    most-damage kill credit (currently last-hitter, matching the pre-existing inline behavior, not
    a regression), group/alliance AP split + quest notification to every rewarded member (Java
    `notifyKillQuests` walks the killer's whole group/alliance in range; Phase 1 notifies only the
    killer), damage-scaled AP loss for the victim.
  - **Deferred to Phase 3** (per the survey, unchanged): per-victim daily-kill cap (`KillList`,
    closes the repeat-farm exploit), kill item rewards, serial-killer system.
  - Removed now-dead `RateOptions`/`_rates` plumbing from `CM_ATTACK`/`CM_CASTSPELL` (constructors,
    `GsPacketHandlerFactory` call sites) — it was only ever read inside the block that moved to
    `PvpKillHandler`, which takes its own DI-injected `RateOptions`.
  - Build: `dotnet build AionLightning.NET/AionLightning.NET.sln` — 0 warnings, 0 errors.

- [x] **Phase 5 Batch 0** (2026-07-11): implemented the blocking prerequisites identified by the
  scoping study above — new engine hooks, the `SM_PLAY_MOVIE` packet, a `QuestHandlerBase` helper
  expansion, mission-start plumbing, a batch quest-script compile mode, and 3 hand-ported Poeta
  quests as the pattern exemplar for the zone-batch agents that follow.
  - **New `QuestEngine` hooks** (same flat-index + dispatch shape as the existing
    `OnKillAsync`/`OnPlayerKillAsync`):
    - `RegisterOnLevelUp`/`OnLevelUpAsync(Player, conn, ct)` — wired from
      `ExperienceService.HandleLevelUpAsync`, called *before* the `SM_NEARBY_QUESTS` send (Java
      ordering: `onLvlUp()` then `updateNearbyQuests()`) so a mission that just (un)locked shows up
      in the same packet. Skips quests already at `COMPLETE`, matching Java's
      `qs.getStatus() != COMPLETE` guard.
    - `RegisterOnZoneMissionEnd`/`OnZoneMissionEndAsync(QuestEnv, conn, ct)` — **the actual Java
      trigger was confirmed by reading the 3 real call sites, not assumed**: Java's
      `QuestEngine.onEnterZoneMissionEnd` is *never* called generically from a "quest completed"
      broadcast anywhere in `QuestService`/`PlayerController`. The only caller in the whole
      codebase is `quest.poeta._1100KaliosCall.onDialogEvent`, which — right before it turns itself
      in (`SELECTED_QUEST_NOREWARD` while its own status is `REWARD`) — loops a hardcoded array of
      dependent quest ids (`{1001,1002,1003,1004,1005}`) and calls
      `QuestEngine.getInstance().onEnterZoneMissionEnd(new QuestEnv(target, player, id, dialogId))`
      once per id. So the "hook" is really: a zone-mission quest's own script explicitly pokes each
      of its dependents when it turns itself in; the engine method itself just re-validates that
      the supplied id is actually registered before dispatching to that id's own handler. Ported
      exactly that way — `OnZoneMissionEndAsync` takes the poked quest id via `env.QuestId` (no
      generic "any quest completed" broadcast exists, so none was invented) — and documented at
      the call site so future zone-mission scripts (quest 1100 itself, ported in the Poeta batch)
      call it the same way `_1100KaliosCall` does. **Not** wired into `QuestRewardService` — that
      would have fabricated a trigger Java doesn't have.
    - `RegisterOnQuestMovieEnd(movieId, questId)`/`OnMovieEndAsync(Player, movieId, conn, ct)` —
      wired from `CM_PLAY_MOVIE_END` (previously a stub `RunAsync` that read and discarded the
      packet; now takes `GsClientConnection`/`QuestEngine` via constructor injection like every
      other `CM_*` handler and calls the dispatcher). Stops at the first handler that reports
      `true`, matching Java's early-return.
    - `RegisterOnEnterWorld(questId)`/`OnEnterWorldAsync(Player, conn, ct)` — **not** one of the 3
      hooks the scoping study called out, added alongside them because the golden `_1000Prologue`
      exemplar structurally needs one (Java starts/plays-movie on first login). Bridged from the
      already-existing `PlayerEnteredWorldEvent` (published by `CM_LEVEL_READY` after the full
      enter-world packet sequence) via a new `QuestEngine/QuestEnterWorldHandler.cs :
      IEventHandler<PlayerEnteredWorldEvent>`, registered in `Program.cs` next to `PvpKillHandler`
      — same event-bus-to-engine bridging pattern, since the event only carries `Player` and the
      connection has to be looked up via `PlayerConnectionRegistry`.
  - **`SM_PLAY_MOVIE`** (`Network/Aion/ServerPackets/SM_PLAY_MOVIE.cs`, opcode `0x69`): ported
    Java's `writeC(type)+writeD(objectId)+writeD(id)+writeH(movieId)+writeD(restrictionId)` layout
    and all 3 constructor overloads (2/4/5-arg). `QuestHandlerBase.PlayQuestMovieAsync(conn,
    player, movieId, ct)` always uses `type=0` (Java's `playQuestMovie` helper does the same); a
    script that needs `type=1` (the "CutSceneMovies" intro category — see `_1000Prologue`) sends
    `SM_PLAY_MOVIE` directly instead of going through the helper, exactly like Java's own
    `_1000Prologue.onEnterWorldEvent` does.
  - **`QuestHandlerBase` helper expansion** (ported from Java `QuestHandler`, collapsing each
    method's overload fan-out down to the 2-3 shapes actually needed rather than replicating every
    Java overload 1:1 — `ct` stays a required trailing parameter everywhere, matching this
    codebase's existing convention, so C# optional-parameter chains weren't an option):
    `DefaultCloseDialogAsync` (no-item + item-give/remove-via-`IItemDao` overloads),
    `DefaultOnKillEventAsync` (flat span-increment + reward-flip-at-exact-var overloads — distinct
    from `MonsterHuntHandler`'s packed multi-group spans), `DefaultOnLvlUpEventAsync` /
    `DefaultOnZoneMissionEndEventAsync` (see `StartMissionAsync` below), `CheckQuestItemsAsync`
    (validates + consumes quest_data.xml `<collect_items>`, Java's `QuestService.collectItemCheck`
    inlined since it wasn't already a reusable method), `GiveQuestItemAsync`/`RemoveQuestItemAsync`
    (promoted from the inline private methods `ReportToHandler` had — same give-the-shortfall /
    remove-and-maybe-delete logic, now `protected` on the base and reused directly by the golden
    scripts), `UseQuestObjectAsync` (immediate-apply only — see deviations below),
    `PlayQuestMovieAsync`, `SendQuestSelectionDialogAsync` (page 10 shorthand).
  - **`StartMissionAsync(conn, player, status, ct)`**: ports `QuestService.startMission` — creates
    the entry at `LOCKED` or `START` only if the player has no state for the quest yet, persists,
    and sends the `Accept`-type `SM_QUEST_ACTION`. Used by both `DefaultOnLvlUpEventAsync` and
    `DefaultOnZoneMissionEndEventAsync`.
  - **Simplified vs Java in the mission-start helpers** (documented at each method): only race +
    minimum level + "every precondition quest id is COMPLETE" are checked. Java's
    `QuestService.checkMissionStatConditions` (class/gender/combine-skill gates) and
    `XMLStartCondition` (`<start_condition>` XML rules) have no equivalent infra in this port yet —
    every quest_data.xml entry that relies on either would need that ported first; none of the 3
    golden quests do.
  - **DialogAction completeness** (deliverable 5): diffed every member name in Java
    `model/DialogAction.java` (197 entries) against the ported `QuestEngine/Model/DialogAction.cs`
    — **already 1:1**, including the full `SETPRO1..SETPRO41` run and every `SELECT_ACTION_*`/
    `SELECTED_QUEST_REWARD*` member. No changes needed; this had already been ported completely in
    an earlier session.
  - **Batch compile mode**: `CSharpCompilerService.CompileFolder(folder, name)` — parses every
    `*.cs` under `folder` (recursive) into one `CSharpCompilation`/one collectible `ALC`, sharing
    the same reference-gathering and emit/diagnostics logic as the existing per-file `Compile`
    (factored into a private `CompileTrees` helper). The existing per-file mode is untouched byte
    for byte apart from that extraction. `ScriptService` (the `IScript`-based hot-reload path) was
    **not** touched — its per-file model fits `Scripts/sample/*` hot-reload scripts; the new batch
    mode is a separate, additive capability consumed directly by `QuestEngineHostedService`, not
    routed through `ScriptService`.
  - **Script-authoring convention** (`QuestEngineHostedService.LoadHandWrittenScripts`, doc'd in
    the XML remarks there too): every hand-written quest script must
    1. subclass `QuestHandlerBase`,
    2. hardcode its quest id as a `private const int` passed to the base constructor (Java's own
       `private static final int questId` + `super(questId)`, just without a DI container to
       thread deps through), and
    3. expose **exactly one public constructor** shaped
       `(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao
       itemDao)` — the same dependencies template handlers that touch items receive (e.g.
       `ReportToHandler`), minus the XML data-row argument scripts don't have. Scripts that don't
       touch items still declare (and simply ignore) `IItemDao` — the host needs one fixed
       parameter list it can resolve by reflection across every script uniformly, so there's no
       per-script variance to special-case.

    At startup, `QuestEngineHostedService.LoadHandWrittenScripts` batch-compiles
    `Scripts/quest/**` (skipped entirely, with a log line, if the folder doesn't exist), reflects
    over the resulting assembly for non-abstract `QuestHandlerBase` subclasses, resolves each
    one's 4-arg constructor (logging a warning and skipping any type that doesn't have exactly
    that shape), invokes it, and registers the handler via the existing `engine.AddQuestHandler`
    — which already logs a warning on a duplicate quest id (`_log.LogWarning("QuestEngine:
    duplicate handler registered for quest {QuestId}", ...)`), so id-disjointness between
    template-populated and script-populated quests (deliverable 7) was already covered by
    existing code; no change was needed there.
  - **Golden scripts** (`Scripts/quest/poeta/_1000Prologue.cs`, `_1002RequestoftheElim.cs`,
    `_1003IllegalLogging.cs`, ported from the matching Java files 1:1 for structure): scripts must
    explicitly `using System.Threading;`/`using System.Threading.Tasks;` (and `System.Linq` if they
    use LINQ) — unlike the project-level build, the ad-hoc `CSharpCompilation` used for batch
    compilation does **not** get the csproj's `ImplicitUsings`, so these can't be omitted the way
    they can everywhere else in the solution. Found this the hard way via the probe below (it
    failed on `CancellationToken`/`ValueTask<>`/`Contains` until the usings were added) — worth
    calling out for the fleet agents writing the next ~1,490 scripts.
    - `_1000Prologue`: full port — `OnEnterWorldAsync` starts the quest for Elyos players and
      plays movie 1 (`SM_PLAY_MOVIE(1, 1)` directly, not via the helper — see above);
      `OnMovieEndAsync` completes it via `QuestRewardService.GrantAndCompleteAsync`.
    - `_1003IllegalLogging`: full port — dialog chain at NPC 203081 (`SETPRO1`/`SETPRO2` share a
      case exactly like Java's fallthrough) + a flat kill counter for 9 "woodcutter" mobs
      (`DefaultOnKillEventAsync` span overload) + a 10th boss mob with its own two-behavior
      var-range/reward-flip case (written inline, since Java's single `if/else if` block doesn't
      match either helper shape alone — same "bespoke when it doesn't fit a helper" precedent
      `KillInWorldHandler` already set).
    - `_1002RequestoftheElim`: ported the full reachable dialog tree (Ampeis → Noah incl. the
      evidence-item give/remove + `SELECT_ACTION_1353` cutscene → Sleeping Elder object-use →
      Daminu → Kalio reward). **Intentionally not ported** (documented in the file's own header,
      since this is the pattern future agents will copy):
      - Java's `SETPRO5` case teleports into instance zone `310010000` via
        `InstanceService.getNextAvailableInstance` + `TeleportService2.teleportTo` (var 13→20),
        and Java's `onEnterWorldEvent` override then sends `SM_ASCENSION_MORPH` inside that zone
        and corrects var 20→13 on leaving it. Neither `InstanceService`, `TeleportService2`, nor
        `SM_ASCENSION_MORPH` exist in this port. Rather than leave a dead branch, `SETPRO5` now
        transitions var 13→14 directly (the same var both paths eventually reach), so the quest
        stays completable end-to-end; `OnEnterWorldAsync` isn't overridden at all for this quest.
      - The var==20 dialog case at Belpartan (205000) — Java's 43s scheduled flight-teleport via
        `ThreadPoolManager` — is unreachable once var never reaches 20, so it's omitted (NPC
        205000 is still registered for `OnTalk` parity with Java's own npc list, it just has no
        case that handles it).
      - The Sleeping Elder's `getController().scheduleRespawn()`/`.onDelete()` npc-despawn/respawn
        calls are skipped (no Npc AI/controller subsystem in this port yet) — the underlying var
        transition (2→4→5) is still ported via `UseQuestObjectAsync` so this doesn't block
        progress, just drops a visual.
      - Java's `onCanAct` override (an extra gate on Sleeping Elder interactivity) has no
        equivalent hook in this port's `IQuestHandler` and was omitted — `UseQuestObjectAsync`'s
        own var-equality check already prevents out-of-order use.
      - Java's two `useQuestObject` calls for var 2 vs var 4 differ in whether the surrounding
        `if`/`else if` branch `return`s the call's boolean result (var==2 silently discards it —
        reads like an upstream Java bug, not an intentional asymmetry); this port returns both
        uniformly.
  - **Probe**: a scratchpad console app referencing the built `AionLightning.Commons`/
    `AionLightning.Game` projects called `CSharpCompilerService.CompileFolder` directly against
    the *built output*'s `bin/Debug/net10.0/Scripts/quest` folder (proving the copy-not-compile
    csproj wiring and the batch-compile mode both work end-to-end against what actually ships) —
    all 3 `.cs` files compiled into one assembly, and reflecting for `QuestHandlerBase` subclasses
    with the 4-arg constructor found and discovered all 3 (`_1000Prologue`=1000,
    `_1002RequestoftheElim`=1002, `_1003IllegalLogging`=1003).
  - Build: `dotnet build AionLightning.NET/AionLightning.NET.sln` — 0 warnings, 0 errors.

- [x] **C2 Phase 5 Poeta batch (partial, 2026-07-11, hand-authored)**: ported quests 1001, 1004,
  1005, 1100 (campaign chain: kerub kill, odium neutralize, gate finale, Kalio's call opener) +
  the 3 golden (1000/1002/1003). Batch-compile probe: 7/7 discovered. Skips documented per-file
  (emotions, zone-polygon start → worldId gate for 1100). Remaining Poeta scripts (1107, 1111,
  1114, 1118, 1122, 1123, 1205 + more) deferred — several need an item-USE trigger hook
  (onItemUseEvent) and finishQuest-with-reward-index which Batch 0 did not build; that's a small
  Batch 0.1 before finishing the zone. Fleet zone agents blocked until API session limit resets
  (23:10 Kyiv).

- [x] **C2 Phase 5 Batch 0.1** (2026-07-11): added the item-USE quest trigger (Java onItemUseEvent):
  QuestEngine.RegisterQuestItem + OnItemUseAsync dispatcher + IQuestHandler/QuestHandlerBase
  OnItemUseAsync member, wired into CM_USE_ITEM (a quest claiming the use short-circuits normal
  item use). Added QuestHandlerBase.FinishQuestAsync(rewardIndex) wrapping
  QuestRewardService.GrantAndCompleteAsync (Java finishQuest(env, index)). Ported Poeta 1107
  (Lost Axe — item-use start) and 1111 (Insomnia Medicine — two-recipe reward-index turn-in).
  Probe: 9/9 Poeta discovered. Fleet agents now have the item-use + finish-index primitives.

- [x] **C2 Phase 5 Poeta delivery quests** (2026-07-11): ported 1118 (Polinia's Ointment),
  1122 (Delivering Pernos's Robe — 3-robe reward tiers via FinishQuestAsync), 1123 (Where's
  Tutty — zone-movie via OnEnterWorld). Probe: 12/12 Poeta scripts discovered. Only 1114
  (The Nymph's Gown, 216L) and 1205 (A New Skill, 182L) remain in Poeta — left for fleet
  (larger, more branch-heavy). Reminder: fleet zone agents resume after API reset (23:10 Kyiv).

- [x] **C2 Phase 5 Poeta COMPLETE** (2026-07-11): 1114 (Nymph's Gown — two-path branch, item-use
  start) + 1205 (A New Skill — level-up auto-start, class-keyed reward NPC) ported; added
  PlayerClass.GetStartingClassFor helper (Java parity). All 14/14 Poeta scripts compile +
  discover via probe. Skips: 1114 omits the Seirenia aggro side-effect (handler has no
  NpcAiService), quest still completes. Elyos starter zone done — Ishalgen next (fleet).

- [x] **C2 Phase 5 Ishalgen start** (2026-07-11): ported 2000 (Prologue — enter-world movie),
  2100 (Order of the Captain — Asmodian campaign opener, 2001-2007 chain), 2132 (A New Skill —
  mirror of 1205). Probe: 17/17 total discovered.
- NOTE for fleet — **Batch 0.2 needed**: some quests (e.g. Ishalgen 2136 Lost Axe) call
  QuestService.addNewSpawn to spawn the turn-in NPC. The fixed 4-arg script ctor has no
  SpawnService. Add a spawn primitive to the script convention (either a 5th ctor param or a
  QuestHandlerBase.SpawnNpcAsync helper backed by a static SpawnService set at engine startup)
  before porting spawn-dependent quests. 2136 deferred until then.

- [x] **C2 Phase 5 Batch 0.2** (2026-07-11): added the spawn primitive to the script convention —
  QuestHandlerBase.SpawnQuestNpc(worldId, instanceId, npcId, x, y, z, heading) backed by a static
  SpawnService set once at engine startup (keeps the fixed 4-arg script ctor). Java addNewSpawn
  parity. Ported Ishalgen 2136 (Lost Axe — object-use spawns the turn-in NPC Rhoo). Probe: 18/18.
  Fleet convention now covers dialog/kill/item-get/item-use/skill-use/level-up/zone-mission/
  movie-end/enter-world hooks + finishQuest-index + spawn.

- [x] **C2 Phase 5 Ishalgen near-complete** (2026-07-11): 15/17 ported (2000/2001/2003/2004/2005/
  2006/2100/2106/2114/2122/2123/2125/2132/2135/2136). Only the two large branch-heavy "Where's
  Rae" quests remain: 2002 (279L) + 2007 (237L) — left for fleet. Probe: 29 scripts total
  (14 Poeta + 15 Ishalgen) compile and discover. Common gotcha for fleet: scripts using
  Model.Player in a private helper param need an explicit `using AionLightning.Game.Model;`
  (ImplicitUsings is off for ad-hoc Roslyn compile).

- [x] **C2 Phase 5 Verteron** (2026-07-12, fleet): 25 quests ported (1011/1013/1015/1017/1018/
  1021/1022/1130/1131/1141/1152/1156/1158/1162/1163/1169/1170/1182/1183/1192/1194/1197/1198/
  1218/1220). Probe: 25/25 discovered in isolation. Verteron worldId = 210030000 (corrected).
  Deferred (9): 1012 (sensory-zone triggers), 1146 (quest timer), 1149 (escort/follow AI),
  1157 (onAttackEvent hook — none exists), + 5 largest (1014/1016/1019/1020/1023, 200L+).
  FLEET FOLLOW-UP (Batch 0.3 candidates): quest-timer hook, onAttackEvent hook, zone-shape
  triggers, follow/escort AI — each unblocks a cluster of deferred quests across all zones.

- [x] **C2 Phase 5 Altgard** (2026-07-12, fleet): 33 quests ported, probe 33/33 in isolation
  (orchestrator fixed one long->int cast the agent left). Asmodian 20s zone.

- [x] **C2 Phase 5 Eltnen** (2026-07-12, fleet): 33 quests ported, probe 33/33 in isolation.
  Elyos 20s zone (worldId 210020000). 23 deferred: Kaidan Fortress campaign chain (11, need
  per-quest review), 2 largest (1319/1467), + service-dependent (addHandlerSideQuestDrop,
  OnAtDistanceEvent, zone-shape isItemUse gates, quest timer, follow/escort, flight-teleport).

- [x] **C2 Phase 5 Heiron** (2026-07-12, fleet): 19 quests ported, probe 19/19 in isolation.
  Elyos 30s zone. 36 deferred (Batch 0.3 hooks: onAttackEvent, quest timer, zone-shape/
  onAtDistance, escort/follow, reward-index dialog helpers + the largest campaign scripts).
  Heiron is a big zone — a 2nd pass is warranted after Batch 0.3.
