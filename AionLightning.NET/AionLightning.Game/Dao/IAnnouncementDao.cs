using AionLightning.Game.Model;

namespace AionLightning.Game.Dao;

/// <summary>Port of Java <c>dao.AnnouncementsDAO</c>. Read-only in this port — no admin add/delete UI exists
/// yet, so only the boot-time load is needed (see <c>Services/AnnouncementService.cs</c>).</summary>
public interface IAnnouncementDao
{
    Task<List<Announcement>> LoadAllAsync(CancellationToken ct = default);
}
