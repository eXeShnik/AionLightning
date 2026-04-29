using AionLightning.Game.Model;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class PlayerAppearanceDaoImpl : IPlayerAppearanceDao
{
    private readonly MySqlDataSource _db;

    public PlayerAppearanceDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<PlayerAppearance?> FindByPlayerIdAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<PlayerAppearance>(
            """
            SELECT face AS Face, hair AS Hair, deco AS Deco, tattoo AS Tattoo,
                   face_contour AS FaceContour, expression AS Expression, jaw_line AS JawLine,
                   skin_rgb AS SkinRgb, hair_rgb AS HairRgb, lip_rgb AS LipRgb, eye_rgb AS EyeRgb,
                   face_shape AS FaceShape, forehead AS Forehead,
                   eye_height AS EyeHeight, eye_space AS EyeSpace, eye_width AS EyeWidth,
                   eye_size AS EyeSize, eye_shape AS EyeShape, eye_angle AS EyeAngle,
                   brow_height AS BrowHeight, brow_angle AS BrowAngle, brow_shape AS BrowShape,
                   nose AS Nose, nose_bridge AS NoseBridge, nose_width AS NoseWidth, nose_tip AS NoseTip,
                   cheek AS Cheek, lip_height AS LipHeight, mouth_size AS MouthSize,
                   lip_size AS LipSize, smile AS Smile, lip_shape AS LipShape,
                   jaw_height AS JawHeight, chin_jut AS ChinJut, ear_shape AS EarShape,
                   head_size AS HeadSize, neck AS Neck, neck_length AS NeckLength,
                   shoulders AS Shoulders, shoulder_size AS ShoulderSize,
                   torso AS Torso, chest AS Chest, waist AS Waist, hips AS Hips,
                   arm_thickness AS ArmThickness, arm_length AS ArmLength, hand_size AS HandSize,
                   leg_thickness AS LegThickness, leg_length AS LegLength, foot_size AS FootSize,
                   facial_rate AS FacialRate, voice AS Voice, height AS Height
            FROM player_appearance
            WHERE player_id = @playerId
            """, new { playerId });
    }

    public async Task InsertAsync(int playerId, PlayerAppearance a, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO player_appearance
              (player_id, face, hair, deco, tattoo, face_contour, expression, jaw_line,
               skin_rgb, hair_rgb, lip_rgb, eye_rgb, face_shape, forehead,
               eye_height, eye_space, eye_width, eye_size, eye_shape, eye_angle,
               brow_height, brow_angle, brow_shape, nose, nose_bridge, nose_width, nose_tip,
               cheek, lip_height, mouth_size, lip_size, smile, lip_shape,
               jaw_height, chin_jut, ear_shape, head_size, neck, neck_length,
               shoulders, shoulder_size, torso, chest, waist, hips,
               arm_thickness, arm_length, hand_size, leg_thickness, leg_length, foot_size,
               facial_rate, voice, height)
            VALUES
              (@playerId, @Face, @Hair, @Deco, @Tattoo, @FaceContour, @Expression, @JawLine,
               @SkinRgb, @HairRgb, @LipRgb, @EyeRgb, @FaceShape, @Forehead,
               @EyeHeight, @EyeSpace, @EyeWidth, @EyeSize, @EyeShape, @EyeAngle,
               @BrowHeight, @BrowAngle, @BrowShape, @Nose, @NoseBridge, @NoseWidth, @NoseTip,
               @Cheek, @LipHeight, @MouthSize, @LipSize, @Smile, @LipShape,
               @JawHeight, @ChinJut, @EarShape, @HeadSize, @Neck, @NeckLength,
               @Shoulders, @ShoulderSize, @Torso, @Chest, @Waist, @Hips,
               @ArmThickness, @ArmLength, @HandSize, @LegThickness, @LegLength, @FootSize,
               @FacialRate, @Voice, @Height)
            """,
            new { playerId, a.Face, a.Hair, a.Deco, a.Tattoo, a.FaceContour, a.Expression, a.JawLine,
                  a.SkinRgb, a.HairRgb, a.LipRgb, a.EyeRgb, a.FaceShape, a.Forehead,
                  a.EyeHeight, a.EyeSpace, a.EyeWidth, a.EyeSize, a.EyeShape, a.EyeAngle,
                  a.BrowHeight, a.BrowAngle, a.BrowShape, a.Nose, a.NoseBridge, a.NoseWidth, a.NoseTip,
                  a.Cheek, a.LipHeight, a.MouthSize, a.LipSize, a.Smile, a.LipShape,
                  a.JawHeight, a.ChinJut, a.EarShape, a.HeadSize, a.Neck, a.NeckLength,
                  a.Shoulders, a.ShoulderSize, a.Torso, a.Chest, a.Waist, a.Hips,
                  a.ArmThickness, a.ArmLength, a.HandSize, a.LegThickness, a.LegLength, a.FootSize,
                  a.FacialRate, a.Voice, a.Height });
    }
}
