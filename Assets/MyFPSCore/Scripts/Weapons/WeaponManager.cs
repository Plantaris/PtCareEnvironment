using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Start")]
        public bool autoSelectOnStart = true;
        public WeaponType defaultWeapon = WeaponType.Syringe;

        [Header("Assign the actual weapon components")]
        public MonoBehaviour[] weaponComponents; // e.g., SyringeGunAdapter, ScalpelWeapon, PistolGunAdapter, RocketLauncherGunAdapter

        [Header("Input")]
        public KeyCode fireKey = KeyCode.Mouse0;
        public KeyCode slot1 = KeyCode.Alpha1;   // Syringe
        public KeyCode slot2 = KeyCode.Alpha2;   // Scalpel
        public KeyCode slot3 = KeyCode.Alpha3;   // Pistol
        public KeyCode slot4 = KeyCode.Alpha4;   // RocketLauncher

        [SerializeField] WeaponShooter shooter;   // drag your WeaponShooter here (or auto-find in Awake)

        [Header("HUD (optional)")]
        public MyFPSCore.UI.HUDController hud;   // use your HUDController

        private readonly Dictionary<WeaponType, IWeapon> _weapons = new();
        private IWeapon _current;
        private readonly HashSet<WeaponType> _unlocked = new HashSet<WeaponType>();
        public bool IsUnlocked(WeaponType type) => _unlocked.Contains(type);
        public bool HasWeapon(WeaponType type) => _weapons.ContainsKey(type);

        void Awake()
        {
            _weapons.Clear();

            foreach (var mb in weaponComponents)
            {
                if (!mb) continue;

                // Accept either the IWeapon component itself or any sibling on the same GO
                IWeapon w = mb as IWeapon;
                if (w == null) w = (mb as Component)?.GetComponent<IWeapon>();

                if (w != null)
                {
                    _weapons[w.Type] = w;
                    w.OnDeselected();
                }
                else
                {
                    Debug.LogWarning($"[WeaponManager] '{mb.name}' does not have an IWeapon component.", mb);
                }
            }

            // try to find the WeaponShooter automatically
            if (!shooter) shooter = GetComponent<WeaponShooter>();
            if (!shooter) shooter = GetComponentInChildren<WeaponShooter>(true);
            if (!shooter) shooter = GetComponentInParent<WeaponShooter>();

            // NEW: auto-find HUD if not wired in Inspector
            if (!hud) hud = FindObjectOfType<MyFPSCore.UI.HUDController>(true);

            // FIX: unlock + select default weapon on start (instead of selecting while locked)
            if (autoSelectOnStart && _weapons.ContainsKey(defaultWeapon))
            {
                UnlockWeapon(defaultWeapon, selectAfter: true); // this both unlocks and selects, then UpdateHUD() runs
            }
            else
            {
                LockAllWeaponsAtStart();
            }
        }

        void Start()
        {
            foreach (var kv in _weapons)
                Debug.Log($"[WM] Registered: {kv.Key} -> {((MonoBehaviour)kv.Value).name}");
        }

        void Update()
        {
            if (_current != null && Input.GetKeyDown(fireKey) && _current.CanFire())
            {
                _current.Fire();
                UpdateHUD();
            }

            if (Input.GetKeyDown(slot1)) SelectWeapon(WeaponType.Syringe);
            if (Input.GetKeyDown(slot2)) SelectWeapon(WeaponType.Scalpel);
            if (Input.GetKeyDown(slot3)) SelectWeapon(WeaponType.Pistol);
            if (Input.GetKeyDown(slot4)) SelectWeapon(WeaponType.RocketLauncher);
        }

        public void SelectWeapon(WeaponType type)
        {
            if (!_weapons.TryGetValue(type, out var w)) return;
            if (!_unlocked.Contains(type)) { /* locked: ignore keypress */ return; }

            if (_current != null) _current.OnDeselected();
            _current = w;
            _current.OnSelected();
            UpdateHUD();

            // Once weapon is active in the hierarchy, calls the correct lunge
            UpdateShooterLungeForCurrent();
        }

        public void AddAmmo(WeaponType type, int amount)
        {
            if (_weapons.TryGetValue(type, out var w) && w is IAmmoWeapon ammoWep)
            {
                ammoWep.AddAmmo(amount);
                if (_current == w) UpdateHUD();
            }
        }

        public void UnlockWeapon(WeaponType type, bool selectAfter = true)
        {
            if (!_weapons.TryGetValue(type, out var w)) return;

            _unlocked.Add(type);

            // ensure the weapon GO is enabled
            var go = ((MonoBehaviour)w).gameObject;
            if (!go.activeSelf) go.SetActive(true);

            if (selectAfter) SelectWeapon(type);
        }

        public void LockAllWeaponsAtStart()
        {
            _unlocked.Clear();
            // Optional: force all children off (in case they were left on)
            foreach (var kv in _weapons)
                ((MonoBehaviour)kv.Value).gameObject.SetActive(false);
        }

        public void RegisterWeapon(IWeapon w, bool selectAfter = false)
        {
            if (!_weapons.ContainsKey(w.Type)) _weapons[w.Type] = w;
            w.OnDeselected();
            if (selectAfter) SelectWeapon(w.Type);
        }

        void UpdateHUD()
        {
            if (!hud || _current == null) return;

            // Weapon name
            hud.SetWeapon(GetWeaponDisplayName(_current.Type));

            // Ammo: pass current + max for now (HUD renders ∞ if negative)
            hud.SetAmmo(_current.CurrentAmmo, _current.MaxAmmo);
        }

        private static readonly System.Collections.Generic.Dictionary<WeaponType, string> _displayNames =
    new()
    {
        { WeaponType.Syringe,        "Syringe" },
        { WeaponType.Scalpel,        "Scalpel" },
        { WeaponType.Pistol,         ".40 Cal" },       
        { WeaponType.RocketLauncher, "Rocket Launcher" }
    };

        private static string GetWeaponDisplayName(WeaponType t)
            => _displayNames.TryGetValue(t, out var name) ? name : t.ToString();

        void UpdateShooterLungeForCurrent()
        {
            if (!shooter || _current == null) return;

            // get the Transform of the selected weapon component
            var mb = _current as MonoBehaviour;
            if (!mb) return;

            // find the lunge that lives under THIS weapon’s viewmodel
            var activeLunge = mb.transform.GetComponentInChildren<SimpleWeaponLunge>(true);

            // tell WeaponShooter to use it (null is fine if a weapon doesn’t have lunge)
            shooter.SetLunge(activeLunge);
        }
    }
}
