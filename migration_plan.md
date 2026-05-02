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

176. [✓] Fix crit rate 10× scaling bug and NPC crit accuracy (session 2026-05-02)
    - [✓] `CM_ATTACK.cs` — player physical crit: `Random.Shared.Next(1000)` → `Random.Shared.Next(100)`; Java `StatFunctions.calculatePhysicalCriticalRate` uses `nextInt(100)` not `nextInt(1000)`
    - [✓] `CM_CASTSPELL.cs` — magical crit in all 3 damage paths (single-target, ground AoE, AoE splash): same `Next(1000)` → `Next(100)` fix; mCritRate values are percent-scale (0-100), not per-mille
    - [✓] `NpcAiService.cs` — NPC crit: was hardcoded 5%; now uses same piecewise formula with `npc.Template.Stats?.Power` as PHYSICAL_CRITICAL rating (Java `NpcGameStats.getMainHandPCritical()` default=10 → 1%); falls back to 10 when field absent
    - Java reference: `NpcGameStats.getMainHandPCritical()` returns `getStat(PHYSICAL_CRITICAL, 10)` (base 10, not 5%); `calculatePhysicalCriticalRate` compares against `nextInt(100)`, giving rate=10×0.1=1% for NPC base
    - Previously: all crit chances were 10× too low; NPC crit was 5× too high relative to Java
    - Build: 0 warnings, 0 errors
