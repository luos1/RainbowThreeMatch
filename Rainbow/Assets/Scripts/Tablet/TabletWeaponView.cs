using UnityEngine;
using UnityEngine.UI;

internal readonly struct TabletWeaponFeedback
{
    static readonly string[] ShotClips = { "laserSmall_000", "laserSmall_003", "laserRetro_000", "laserLarge_000" };
    static readonly Color[] GlowColors =
    {
        Color.clear, new Color32(86, 227, 210, 255), new Color32(255, 176, 83, 255), new Color32(183, 226, 255, 255)
    };

    readonly int level;

    internal TabletWeaponFeedback(int attackLevel) => level = Mathf.Clamp(attackLevel, 0, ShotClips.Length - 1);

    internal bool Upgraded => level > 0;
    internal string ShotClipPath => "Tablet/SciFiSounds/" + ShotClips[level];
    internal Color Glow => GlowColors[level];
    internal float CollarWidth => .022f + .010f * level;
    internal float FlashSize => .056f + .014f * level;
    internal float Pitch => 1.12f - .08f * level;
    internal float Volume => .18f + .023f * level;
    internal float DefeatScale => 1f + .23f * level;
}

internal sealed partial class TabletView
{
    readonly Image[] cannonCollars = new Image[3];
    readonly Image[] cannonCoils = new Image[3];
    readonly Image[] hullBraces = new Image[2];
    readonly Image[] defeatShockwaves = new Image[TabletCombat.EffectCapacity];
    TabletWeaponFeedback weaponFeedback;
    int hullLevel, observedShots;
    AudioClip weaponClip;
    AudioSource weaponVoice;

    void BuildUpgradeVisuals()
    {
        for (int i = 0; i < hullBraces.Length; i++)
        {
            var brace = Graphic(arena, "Carrier armor brace " + (i + 1), 0, 0, 0, 0, Color.white);
            hullBraces[i] = brace;
            Graphic(brace.transform, "Armor seam", .07f, .14f, .93f, .28f, new Color32(169, 223, 247, 255));
        }
        for (int i = 0; i < cannonCollars.Length; i++)
        {
            var collar = Graphic(arena, "Carrier cannon collar " + (i + 1), 0, 0, 0, 0, new Color32(39, 54, 70, 255));
            collar.rectTransform.pivot = new Vector2(.5f, 1f);
            cannonCollars[i] = collar;
            var coil = Graphic(collar.transform, "Power coil", .09f, .26f, .91f, .85f, Color.white);
            cannonCoils[i] = coil;
            Graphic(coil.transform, "Barrel", .23f, 0, .77f, 1, new Color32(20, 29, 42, 255));
            Graphic(collar.transform, "Muzzle lip", .23f, .85f, .77f, 1, new Color32(129, 148, 164, 255));
        }
        for (int i = 0; i < defeatShockwaves.Length; i++)
            defeatShockwaves[i] = Entity("Defeat shockwave " + (i + 1), ring);
    }

    internal void ConfigureUpgrades(int attackLevel, int hullLevel)
    {
        this.hullLevel = hullLevel;
        weaponFeedback = new TabletWeaponFeedback(attackLevel);
        weaponClip = Resources.Load<AudioClip>(weaponFeedback.ShotClipPath);
        observedShots = 0;
        if (weaponVoice != null) weaponVoice.Stop();

        for (int i = 0; i < cannonCollars.Length; i++)
        {
            SetActive(cannonCollars[i], weaponFeedback.Upgraded);
            cannonCoils[i].color = weaponFeedback.Glow;
        }
        foreach (var brace in hullBraces)
        {
            SetActive(brace, hullLevel > 0);
            brace.color = Color.Lerp(new Color32(55, 78, 101, 255), new Color32(135, 172, 195, 255), hullLevel / 3f);
        }
    }

    void RenderUpgrades(TabletCombat state, float unit)
    {
        if (weaponFeedback.Upgraded)
        {
            for (int i = 0; i < cannonCollars.Length; i++)
                Place(cannonCollars[i], state.CarrierMuzzle(i), new Vector2(weaponFeedback.CollarWidth, .045f) * unit);
        }
        if (hullLevel > 0)
        {
            for (int i = 0; i < hullBraces.Length; i++)
            {
                Vector2 position = state.CarrierMuzzle(i + 1) - new Vector2(0, .050f);
                Place(hullBraces[i], position, new Vector2(.047f + .013f * hullLevel, .015f + .004f * hullLevel) * unit);
            }
        }

        bool justFired = state.FiredShots > observedShots;
        observedShots = state.FiredShots;
        if (state.Result != TabletResult.Playing)
        {
            if (weaponVoice != null && weaponVoice.isPlaying) weaponVoice.Stop();
            return;
        }
        if (!justFired || weaponClip == null) return;

        // One separate voice preserves the puzzle cues and keeps laser transients from stacking.
        if (weaponVoice == null)
        {
            weaponVoice = safeRoot.gameObject.AddComponent<AudioSource>();
            weaponVoice.playOnAwake = false;
            weaponVoice.spatialBlend = 0f;
        }
        if (weaponVoice.isPlaying) return;
        weaponVoice.clip = weaponClip;
        weaponVoice.pitch = weaponFeedback.Pitch;
        weaponVoice.volume = weaponFeedback.Volume;
        weaponVoice.Play();
    }

    void RenderDefeatShockwave(int index, TabletCombat.Effect effect, float unit)
    {
        Image shockwave = defeatShockwaves[index];
        bool visible = weaponFeedback.Upgraded && effect.Burst && effect.Lifetime > 0;
        SetActive(shockwave, visible);
        if (!visible) return;

        float progress = 1f - effect.Lifetime / effect.Duration;
        Color tint = weaponFeedback.Glow;
        tint.a = .86f * (1f - progress);
        shockwave.color = tint;
        Place(shockwave, effect.Position, unit * effect.Size * weaponFeedback.DefeatScale * (.8f + progress * 1.2f));
    }
}
