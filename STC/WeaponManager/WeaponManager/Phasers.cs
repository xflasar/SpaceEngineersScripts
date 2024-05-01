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
using VRageRender;
using VRageRender.Messages;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Dictionary<MyDetectedEntityInfo, float> enemies = new Dictionary<MyDetectedEntityInfo, float>();

        private List<Phaser> phasers = new List<Phaser>();
        private List<Phaser> _reloadingPhasers = new List<Phaser>();

        private double _currentRechargingPowerUse = 0;

        private double _currPower = 0;
        private double _maxAvailablePower = 0;

        private bool _hydroEnginesRunning = false;
        private bool _batteriesDischarging = false;

        private int _selectiveCounterPhasers = 4;

        private bool firstRun = false;
        private bool forceReload = false;

        private long _target = 0;
        private long _lastTargetBlock = 0;

        private class Phaser
        {
            public bool _isLocked;
            public IMyTerminalBlock _phaser;
            public int _firingCycleTimer;
            public bool _recharging;
            public double _currentPower;
            public float _minPower;
            public bool _readyToFire;
            public long _target;
            public int _maxChargePower;
            public bool _firing;
            public double _chargeSize;
            public double _minChargePower;
            public double _currentPowerUsing;
            public bool _passiveRecharging;
            public bool _activeRecharging;

            public Phaser(IMyTerminalBlock phaser, double cPower)
            {
                _phaser = phaser;
                _firingCycleTimer = 10;
                _isLocked = false;
                _recharging = false;
                _readyToFire = true;
                _currentPower = cPower;
                _target = 0;
                _firing = false;
                _passiveRecharging = false;
                _activeRecharging = false;
                _currentPowerUsing = 0;

                if (cPower >= 100 && cPower <= 101)
                {
                    _minPower = 101;
                    _maxChargePower = 1410;
                    _chargeSize = 676800;
                    _minChargePower = CalculateMaxRechargePower(_chargeSize);
                } else if(cPower >= 150 && cPower <= 151)
                {
                    _minPower = 151;
                    _maxChargePower = 2342;
                    _chargeSize = 1108800;
                    _minChargePower = CalculateMaxRechargePower(_chargeSize);
                } else if (cPower >= 200 && cPower <= 201)
                {
                    _minPower = 201;
                    _maxChargePower = 3343;
                    _chargeSize = 1604448;
                    _minChargePower = CalculateMaxRechargePower(_chargeSize);
                }
            }

            double CalculateMaxRechargePower(double chargeSize)
            {
                return chargeSize / (16 * 60);
            }
        }

        private int phasersForceReloadedInit = 0;

        void CheckCorrectSettings(IMyTerminalBlock phas, MyDefinitionId t)
        {
            phas.SetValueBool("OnOff", true);
            double currentPower = api.GetCurrentPower(phas);
            
            if(currentPower == 0)
                while (currentPower == 0)
                {
                    currentPower = api.GetCurrentPower(phas);
                }

            Phaser pha = new Phaser(phas, currentPower);

            phasers.Add(pha);
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
                        CheckCorrectSettings(phas, t);
                    }
                });
            });

            //// ForceReload
            //phasers.ForEach(phaser =>
            //{
            //    if (phaser._maxChargePower == 0)
            //    {
            //        InitPhasers();
            //        return;
            //    }

            //    if (phaser._phaser.GetValueBool("OnOff"))
            //    {
            //        phaser._phaser.ApplyAction("ForceReload");
            //        phaser._readyToFire = false;
            //        phaser._recharging = true;
            //    }
            //    else
            //    {
            //        phaser._phaser.SetValueBool("OnOff", true);
            //        phaser._phaser.ApplyAction("ForceReload");
            //        phaser._readyToFire = false;
            //        phaser._recharging = true;
            //    }
            //});
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
            int activeRechargeCount = 0;
            int passiveRechargeCount = 0;

            // Create a list to store phasers to remove
            if (_currentRechargingPowerUse < 0) _currentRechargingPowerUse = 0;

            if (activeRechargeCount > 0 || passiveRechargeCount > 0)
            {
                BoostWeaponsRechargeWithBatteries();
            }
            
            foreach (Phaser ph in phasers)
            {

                if (!ph._recharging) continue;
                // Calculate the recharge power for the phaser (half of maxChargePower)
                double phaserRechargePower = ph._minChargePower;

                // Check if adding the phaser's recharge power to the current recharging power exceeds the maximum available power
                if ((_currentRechargingPowerUse + phaserRechargePower) > _maxAvailablePower && !ph._activeRecharging)
                {
                    ph._activeRecharging = false;
                    ph._passiveRecharging = true;
                    ph._phaser.ApplyAction("OnOff_Off");
                }
                else if(!ph._activeRecharging)
                {
                    ph._passiveRecharging = false;
                    ph._activeRecharging = true;
                    ph._phaser.ApplyAction("OnOff_On");
                    _currentRechargingPowerUse += phaserRechargePower;
                }

                ph._currentPower = api.GetCurrentPower(ph._phaser);

                if (ph._currentPower > ph._minPower && !(ph._currentPower < ph._minPower - 1))
                {
                    ph._recharging = true;

                    if (ph._activeRecharging)
                    {
                        activeRechargeCount++;
                    }
                    else if (ph._passiveRecharging)
                    {
                        passiveRechargeCount++;
                    }
                }
                else
                {
                    ph._recharging = false;
                    ph._readyToFire = true;
                    ph._firingCycleTimer = 10;
                    ph._activeRecharging = false;
                    ph._passiveRecharging = false;
                    _currentRechargingPowerUse -= ph._minChargePower;
                    ph._currentPowerUsing = 0;
                }
            }

            Echo($"Recharging phasers active: {activeRechargeCount} | Passive:{passiveRechargeCount} || current Charging power: {_currentRechargingPowerUse}");
        }

        void GetSortedEnemies()
        {
            enemies.Clear();
            api.GetSortedThreats(Me, enemies);

            try
            {
                EchoString.Append("Enemy: " + enemies.Count() + "\n\n");
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
                Echo("\n\n");
            }
            catch (Exception e)
            {
                Echo("GetSortedEnemies err: " + e.Message);
            }
            
        }

        int CalculatePhasersCount(double availablePower, double maxChargePower)
        {
            return (int)Math.Floor(availablePower / maxChargePower);
        }

        void AddPhaser()
        {
            //Echo($"Run {_phasersActiveCurrently}|{_phasersMaxRecharge}");
            double powaCur = 0;
            foreach (Phaser ph in phasers)
            {
                _currPower = 0;
                phasers.ForEach(phas =>
                {
                    _currPower += phas._currentPowerUsing;
                });

                if (_currPower + ph._minChargePower > _maxAvailablePower) continue;

                if (ph._readyToFire && !ph._recharging && !ph._firing)
                {
                    if(!ph._phaser.GetValueBool("OnOff")) ph._phaser.ApplyAction("OnOff_On");
                    ph._firing = true;
                    ph._currentPowerUsing = ph._minChargePower;
                    //Echo(ph._currentPowerUsing + "+" + ph._minChargePower);
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

        void PrintWeapons()
        {
            int longestPhaserName = 0;

            phasers.ForEach(phaser =>
            {
                if(phaser._phaser.CustomName.Length > longestPhaserName)
                longestPhaserName = phaser._phaser.CustomName.Length;

                if (_debug)
                {
                    EchoString.Append($"Phaser: {phaser._phaser.CustomName}\nFiring cycle: {phaser._firingCycleTimer} => ReadyToFire: {phaser._readyToFire} => IsRecharging: {phaser._recharging} => Recharge power: {phaser._maxChargePower} => Current Draw: {api.GetCurrentPower(phaser._phaser)}\n => MinChargePower: {phaser._minChargePower} => CurrentPowerUsing: {phaser._currentPowerUsing}\n");
                }
                else
                {
                    string status = "Idle";

                    if (phaser._firing) status = "Firing";
                    if (phaser._recharging) status = "Recharging";

                    string targetInfo = "";

                    if (phaser._target != 0) targetInfo = $" || Target: {api.GetWeaponTarget(phaser._phaser, 0).Value.Name}";

                    string phaserName = phaser._phaser.CustomName;

                    if (phaserName.Length < longestPhaserName)
                    {
                        StringBuilder paddedName = new StringBuilder(phaserName);
                        for (int i = 0; i < longestPhaserName - phaserName.Length; i++)
                        {
                            paddedName.Append(' ');
                        }
                        phaserName = paddedName.ToString();
                    }
                    string phaserState = "OFF";

                    if (phaser._phaser.GetValueBool("OnOff"))
                    {
                        phaserState = "ON";
                    }

                    EchoString.Append($"Phaser: {phaserName} [{phaserState}] === {status}" + targetInfo + "\n");
                }
            });
            EchoString.Append("\n\n");
        }

        void FireAtTarget()
        {
            if (firstRun)
            {
                AddPhaser();
                firstRun = false;
            }
            
            BoostPowerWithHydrogenEngines(); // This can be done under check

            if (_selectiveCounterPhasers == 0)
            {
                AddPhaser();
                _selectiveCounterPhasers = 4;
            }
            else
            {
                _selectiveCounterPhasers--;
            }

            Echo($"Next phaser add rotation: {_selectiveCounterPhasers}");

            CheckRechargeWeapons();
            
            // Iterate over phasers
            foreach (Phaser phaser in phasers)
            {
                Phaser ph = phaser;

                if(!ph._firing) continue;

                ph._currentPower = api.GetCurrentPower(ph._phaser);

                if (ph._firingCycleTimer == 3)
                {
                    ph._currentPowerUsing = 0;
                    //Echo("Run Dead - power  " + ph._currentPowerUsing);
                }

                // Check if current phaser is reloading or not; if not, it defaults to minPower of the phaser
                if (ph._currentPower > ph._minPower || ph._firingCycleTimer <= 0)
                {
                    //Echo($"Removing phaser due to {ph._currentPower}/{ph._minPower}");
                    ph._recharging = true;
                    ph._readyToFire = false;
                    ph._firing = false;
                    ph._firingCycleTimer = 10;

                    if (ph._currentPowerUsing > 0)
                    {
                        ph._currentPowerUsing = 0;
                        //Echo("ED " + ph._currentPowerUsing);
                    }
                    continue;
                }

                if(!ph._phaser.GetValueBool("OnOff")) ph._phaser.ApplyAction("OnOff_On");

                // Fire weapon
                try
                {
                    if (api.GetWeaponTarget(ph._phaser).Value.IsEmpty() || !api.GetWeaponTarget(ph._phaser).HasValue)
                    {
                        if (api.CanShootTarget(ph._phaser, _lastTargetBlock, 0))
                        {
                            api.SetWeaponTarget(ph._phaser, _lastTargetBlock);
                        }
                        else
                        {
                            ph._phaser.ApplyAction("OnOff_Off");
                            continue;
                        }
                    }

                    if (!api.CanShootTarget(ph._phaser, api.GetWeaponTarget(ph._phaser).Value.EntityId, 0))
                    {
                        continue;
                    }

                    if (!api.IsWeaponReadyToFire(ph._phaser, 0))
                    {
                        ph._phaser.ApplyAction("OnOff_Off");
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

                if (ph._firing && ph._readyToFire)
                {
                    api.FireWeaponOnce(ph._phaser);
                    ph._firingCycleTimer -= 1;
                    _lastTargetBlock = api.GetWeaponTarget(ph._phaser, 0).Value.EntityId;
                    ph._target = _lastTargetBlock;
                }
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

            Echo($"Max Available Power: {Helper_Functions.FormatText(_maxAvailablePower)} GW || Current Used Power: {Helper_Functions.FormatText(_currPower)} GW\n");
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

        // Batteries
        void BoostWeaponsRechargeWithBatteries()
        {
            _batteries.ForEach(battery =>
            {
                if (battery.CurrentStoredPower > 0)
                {
                    battery.ChargeMode = ChargeMode.Discharge;
                    _batteriesDischarging = true;
                }
                else
                {
                    battery.ChargeMode = ChargeMode.Auto;
                    _batteriesDischarging = false;
                }
            });
        }

        // Hydrogen Engines
        void BoostPowerWithHydrogenEngines()
        {
            double hydroLevelSum = _hydroTanks.Sum(d => d.FilledRatio) / _hydroTanks.Count;
            double a = 0;
            if (hydroLevelSum < 0.1 && _hydrogenEngineGenerators.Aggregate(a, (b, engine) =>
                {
                    if(engine.GetValueBool("OnOff") == true)
                    return 1;
                    return 0;
                }) > 0)
            {
                _hydrogenEngineGenerators.ForEach(hE =>
                {
                    hE.ApplyAction("OnOff_Off");
                    _hydroEnginesRunning = false;
                });
            }

            _hydrogenEngineGenerators.ForEach(hE =>
            {
                if (!hE.GetValueBool("OnOff") && hydroLevelSum > 0.1)
                {
                    hE.ApplyAction("OnOff_On");
                    _hydroEnginesRunning = true;
                }
                else if (hydroLevelSum < 0.1)
                {
                    hE.ApplyAction("OnOff_Off");
                    _hydroEnginesRunning = false;
                }
            });
        } 
    }
}
