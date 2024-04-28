using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Achievements;
using VRage.Game;
using VRageMath;
using VRageRender.Messages;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private int _phasersMaxRecharge = 0;
        private int _phasersActiveCurrently = 0;
        public Dictionary<MyDetectedEntityInfo, float> enemies = new Dictionary<MyDetectedEntityInfo, float>();
        private List<IMyTerminalBlock> _weapsInRechargeQueue = new List<IMyTerminalBlock>();

        private List<Phaser> phasers = new List<Phaser>();
        private List<Phaser> _phasersToShoot = new List<Phaser>();
        private List<Phaser> _reloadingPhasers = new List<Phaser>();

        private List<Phaser> _activeRechargePhasersList = new List<Phaser>();
        private List<Phaser> _passiveRechargePhasersList = new List<Phaser>();

        private int _activeRechargePhasers = 0;

        private double _currPower = 0;
        private double _maxAvailablePower = 0;

        private long _target = 0;

        private class Phaser
        {
            public bool _isLocked;
            public IMyTerminalBlock _phaser;
            public int _firingCycleTimer;
            public bool _recharging;
            public float _currentPower;
            public float _minPower;
            public bool _readyToFire;
            public long _target;
            public int _maxChargePower;

            public Phaser(IMyTerminalBlock phaser, float _mPower, float cPower)
            {
                _phaser = phaser;
                _firingCycleTimer = 10;
                _isLocked = false;
                _recharging = false;
                _readyToFire = true;
                _currentPower = cPower;
                _target = 0;

                if (cPower >= 100 && cPower <= 101)
                {
                    _minPower = 101;
                    _maxChargePower = 1410;
                } else if(cPower >= 150 && cPower <= 151)
                {
                    _minPower = 151;
                    _maxChargePower = 2342;
                } else if (cPower >= 200 && cPower <= 201)
                {
                    _minPower = 201;
                    _maxChargePower = 3343;
                }
            }
        }

        void InitPhasers()
        {

            List<MyDefinitionId> turrets = new List<MyDefinitionId>();
            api.GetAllCoreTurrets(turrets);

            _phasers.ForEach(phas =>
            {
                turrets.ForEach(t =>
                {
                    if (t.SubtypeId.ToString() == phas.BlockDefinition.SubtypeId)
                    {
                        Phaser pha = new Phaser(phas, api.GetMaxPower(t), api.GetCurrentPower(phas));
                        phasers.Add(pha);
                    }
                });
            });
        }

        void ShutdownWeapons()
        {
            phasers.ForEach(w =>
            {
                if (w._phaser.GetValueBool("OnOff"))
                {
                    w._phaser.ApplyAction("OnOff_Off");
                }
            });
        }

        void CheckRechargeWeapons()
        {
            // Create a list to store phasers to remove
            List<Phaser> phasersToRemove = new List<Phaser>();

            // Iterate over _reloadingPhasers
            foreach (Phaser ph in _reloadingPhasers)
            {
                if (_activeRechargePhasers > _phasersMaxRecharge && !_passiveRechargePhasersList.Contains(ph) && !_activeRechargePhasersList.Contains(ph))
                {
                    ph._phaser.ApplyAction("OnOff_Off");
                    _passiveRechargePhasersList.Add(ph);
                    continue;
                }
                else
                {
                    if(!_activeRechargePhasersList.Contains(ph))
                    {
                        _passiveRechargePhasersList.Remove(ph);
                        _activeRechargePhasersList.Add(ph);
                        ph._phaser.ApplyAction("OnOff_On");
                        _activeRechargePhasers++;
                        continue;
                    }
                }
            }

            foreach (Phaser ph in _activeRechargePhasersList)
            {

                ph._currentPower = api.GetCurrentPower(ph._phaser);

                if (ph._currentPower > ph._minPower)
                {
                    ph._recharging = true;
                }
                else
                {
                    ph._recharging = false;
                    ph._readyToFire = true;
                    ph._firingCycleTimer = 10;
                    

                    // Add the phaser to the list of items to remove
                    phasersToRemove.Add(ph);
                }
            }

            // Remove phasers marked for removal
            foreach (Phaser phaserToRemove in phasersToRemove)
            {
                _activeRechargePhasersList.Remove(phaserToRemove);
                _reloadingPhasers.Remove(phaserToRemove);
                _activeRechargePhasers--;
                if (_passiveRechargePhasersList.Count > 0)
                {
                    _activeRechargePhasersList.Add(_passiveRechargePhasersList.Pop());
                    _activeRechargePhasers++;

                }
            }

            Echo($"Recharging phasers: {_activeRechargePhasersList.Count}/{_passiveRechargePhasersList.Count}");
        }


        void GetSortedEnemies()
        {
            enemies.Clear();
            api.GetSortedThreats(Me, enemies);

            try
            {
                EchoString.Append("Enemy: " + enemies.Count() + "\n");
                enemies.Keys.ToList().ForEach(targetEnemies =>
                {
                    bool _isLocked = false;
                    int _weapsActive = 0;

                    phasers.ForEach(phaser =>
                    {
                        if (Vector3D.Distance(api.GetWeaponTarget(phaser._phaser).Value.Position, targetEnemies.Position) < 100)
                        {
                            phaser._isLocked = true;
                            _isLocked = true;
                            _weapsActive++;
                        }
                    });

                    EchoString.Append($"Enemy: {targetEnemies.Name} || Enemy Speed: {targetEnemies.Velocity} || Locked ON: {_isLocked} || Weapons targeting: {_weapsActive}\n");
                });
                Echo("\n");
            }
            catch (Exception e)
            {
                Echo("GetSortedEnemies err: " + e.Message);
            }
            
        }

        void SelectPhasersToFire()
        {
            Echo($"{_phasersActiveCurrently}|{_phasersMaxRecharge}");
            foreach (Phaser ph in phasers)
            {
                Echo($"Phaser: {ph._phaser.CustomName}\nFiring cycle: {ph._firingCycleTimer} => ReadyToFire: {ph._readyToFire} => IsRecharging: {ph._recharging} => Recharge power: {ph._maxChargePower}\n");
                
                if (_currPower + ph._maxChargePower < _maxAvailablePower && !_phasersToShoot.Contains(ph) && ph._readyToFire && !ph._recharging)
                {
                    if(!ph._phaser.GetValueBool("OnOff")) ph._phaser.ApplyAction("OnOff_On");
                    _phasersToShoot.Add(ph);
                    _phasersActiveCurrently++;
                    _currPower += ph._maxChargePower / 2f;
                    continue;
                }
            };
        }

        void FireDisruptors()
        {
            _disruptors.ForEach(dis =>
            {
                if (CanShootTarget(dis))
                {
                    api.FireWeaponOnce(dis);
                }
            });
        }

        void FireAtTarget()
        {
            CheckRechargeWeapons();
            SelectPhasersToFire();

            // Create a list to store phasers to remove
            List<Phaser> phasersToRemove = new List<Phaser>();
            if (_phasersToShoot.Count == 0) _phasersActiveCurrently = 0;
            
            // Iterate over _phasersToShoot
            foreach (Phaser phaser in _phasersToShoot)
            {
                Phaser ph = phaser;

                ph._currentPower = api.GetCurrentPower(ph._phaser);

                if (ph._firingCycleTimer == 7)
                {
                    _phasersActiveCurrently--;
                    _currPower -= ph._maxChargePower / 2f;;
                }

                // Check if current phaser is reloading or not; if not, it defaults to minPower of the phaser
                if (ph._currentPower > ph._minPower)
                {
                    //Echo($"Removing phaser due to {ph._currentPower}/{ph._minPower}");
                    _reloadingPhasers.Add(ph);
                    ph._readyToFire = false;
                    ph._firingCycleTimer = 10;

                    // Add the phaser to the list of items to remove
                    phasersToRemove.Add(ph);
                    continue;
                }

                // Fire weapon
                try
                {
                    if (!api.CanShootTarget(ph._phaser, api.GetWeaponTarget(ph._phaser).Value.EntityId, 0)) continue;

                    if (!api.IsWeaponReadyToFire(ph._phaser, 0))
                    {
                        ph._readyToFire = false;
                        continue;
                    }
                    else
                    {
                        ph._readyToFire = true;
                    }
                }
                catch (Exception e)
                {
                    Echo("FireWeapon: " + e.Message);
                    continue;
                }

                if (ph._firingCycleTimer == 3)
                {
                    ph._phaser.SetValueBool("WC_Overload", true);
                }

                if (ph._readyToFire)
                {
                    api.FireWeaponOnce(ph._phaser);
                    ph._readyToFire = false;
                    ph._firingCycleTimer -= 1;
                }
            }

            // Remove phasers marked for removal
            foreach (Phaser phaserToRemove in phasersToRemove)
            {
                _phasersToShoot.Remove(phaserToRemove);
            }

    }

    void GetMaxAvailablePower()
        {
            double maxOutput = 0;
            _reactors.ForEach(r =>
            {
                maxOutput += r.MaxOutput;
            });

            _batteries.ForEach(b =>
            {
                maxOutput += b.MaxOutput;
            });

            double hydroEnginePower = 0;

            _hydrogenEngineGenerators.ForEach(hE =>
            {
                if (hE.GetValueBool("OnOff"))
                {
                    hydroEnginePower += hE.MaxOutput;
                }
            });

            if (shieldcontrollers.Count == 0) _shieldPowerSet = 0;

            _maxAvailablePower = (maxOutput + hydroEnginePower - (_shieldPowerSet * 1000));

            Echo($"Max Available Power: {_maxAvailablePower} || Current Used Power: {_currPower}");
        }

        bool CanShootTarget(IMyTerminalBlock weapon)
        {
            try
            {
                // Check if the weapon can shoot the target
                return api.CanShootTarget(weapon, api.GetWeaponTarget(weapon).Value.EntityId, 0);
            }
            catch (Exception ex)
            {
                Echo("Error checking if weapon can shoot target: " + ex.Message);
                return false;
            }
        }
    }
}
