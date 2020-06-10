using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Runtime.CompilerServices;

namespace IngameScript
{
    class QuarryContainer : MyGridProgram
    {
        //Modify these values to match those of your game.
        private const string LCDSCREEN = "";
        private const string DRILLNAME = "QuarryDrill";
        private const string PISTONNAME = "QuarryPiston";
        private const float SPEED = 0.2f;
        private const float DISTANCE = 2.5f;

        //Do not modify any code beyond this point!
        private IMyTextPanel m_lcd;
        private Quarry m_quarry;

        public Program()
        {
            List<IMyShipDrill> drills = new List<IMyShipDrill>();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();

            Piston[] pistonArray = new Piston[3];

            GridTerminalSystem.GetBlocksOfType(panels, x => x.CustomName.ToLower().Contains(LCDSCREEN));
            GridTerminalSystem.GetBlocksOfType(drills, x => x.CustomName.ToLower().Contains(DRILLNAME.ToLower()));
            GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(null, x =>
            {
                string name = x.CustomName.ToLower();
                if (name.Contains(PISTONNAME.ToLower()))
                {
                    if (name.Last() == 'x')
                    { pistonArray[0] = new Piston(x); return false; }
                    if (name.Last() == 'y')
                    { pistonArray[1] = new Piston(x); return false; }
                    if (name.Last() == 'z')
                    { pistonArray[2] = new Piston(x); return false; }
                }
                return false;
            });

            if (panels.Count > 0) { m_lcd = panels[0]; }
            m_quarry = new Quarry(pistonArray, drills, Print);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Main()
        {
            m_quarry.Work();
        }
        void Print(object message)
        {
            Echo(message.ToString());
        }

        private class Quarry
        {
            private List<IMyShipDrill> m_drills;
            private Piston[] m_pistons;
            private Action<object> m_reporter;
            private Phase m_quarryPhase;

            private Piston this[Pos index]
            { get { return m_pistons[(int)index]; } }

            public Quarry(Piston[] pistons, List<IMyShipDrill> drills)
                : this(pistons, drills, null)
            { }
            public Quarry(Piston[] pistons, List<IMyShipDrill> drills, Action<object> reporter)
            {
                if (pistons.Length != 3)
                { throw new ArgumentException("Needs to have at least 3 pistons!", "pistons"); }
                if (drills == null || drills.Count < 1)
                { throw new ArgumentException("Needs to have at least 1 drill", "drills"); }

                m_drills = drills.ToList();
                m_pistons = pistons;

                m_reporter = reporter;
                m_quarryPhase = Phase.PreInit;
            }

            public void Work()
            {
                if (m_quarryPhase == Phase.FinishedReset) { return; }

                switch (m_quarryPhase)
                {
                    case Phase.PreInit:
                        Reset(); m_quarryPhase = Phase.Startup;
                        m_reporter?.Invoke("Quarry Initializing...");
                        break;

                    case Phase.Startup:
                        if (Reset())
                        {
                            m_quarryPhase = Phase.Mining;
                            TurnOnDrills(true);
                            m_reporter?.Invoke("Starting Mining...");
                        }
                        break;

                    case Phase.Mining:
                        RunQuarry();
                        break;

                    case Phase.Finished:
                        if (Reset())
                        {
                            m_quarryPhase = Phase.FinishedReset;
                            goto case Phase.FinishedReset;
                        }
                        break;

                    case Phase.FinishedReset:
                        m_reporter?.Invoke("Quarry Finished...");
                        break;
                }
            }

            private void RunQuarry()
            {
                Piston pX = this[Pos.X];
                Piston pY = this[Pos.Y];
                Piston pZ = this[Pos.Z];

                //If any of the pistons are still busy moving, wait.
                if (pX.IsMoving || pY.IsMoving || pZ.IsMoving)
                { m_reporter?.Invoke("Pistons moving..."); return; }

                if (pX.IsFullyExtendedAbsolute)
                {
                    //If X is fully extended, move Z by one, then reverse X.
                    if (pZ.IsFullyExtendedAbsolute)
                    {
                        if (pY.IsFullyExtended)
                        {
                            //All 3 pistons are fully extended.
                            m_quarryPhase = Phase.Finished;
                        }
                        else
                        { MovePiston(pY); }

                        pZ.Reverse();
                    }
                    else
                    { MovePiston(pZ); }

                    pX.Reverse();
                }
                else
                { MovePiston(pX); }
            }
            private void MovePiston(Piston piston)
            {
                m_reporter?.Invoke(string.Format("Moving {0}", piston.MyPiston.CustomName));

                if (piston.Direction)
                { piston.MoveOne(DISTANCE, SPEED); }
                else
                { piston.MoveOne(-DISTANCE, -SPEED); }
            }

            public void TurnOnDrills(bool state)
            {
                foreach (var drill in m_drills)
                {
                    if (state)
                    { drill.ApplyAction("OnOff_On"); }
                    else
                    { drill.ApplyAction("OnOff_Off"); }
                }
            }
            public bool Reset()
            {
                if (!this[Pos.Y].Reset())
                { return false; }
                if (!this[Pos.Z].Reset())
                { return false; }
                if (!this[Pos.X].Reset())
                { return false; }

                TurnOnDrills(false);
                m_reporter?.Invoke("Quarry has reset.");
                return true;
            }

            private enum Pos : int
            { X = 0, Y = 1, Z = 2 }
            private enum Phase : int
            {
                PreInit,
                Startup,
                Mining,
                Finished,
                FinishedReset
            }
        }

        private class Piston
        {
            public IMyPistonBase MyPiston
            { get; }
            public PistonStatus Status
            { get { return MyPiston.Status; } }

            public bool IsRetracted
            { get { return MyPiston.Status == PistonStatus.Retracted; } }
            public bool IsExtended
            { get { return MyPiston.Status == PistonStatus.Extended; } }
            public bool IsFullyExtended
            { get { return MyPiston.Status == PistonStatus.Extended && MyPiston.HighestPosition == MyPiston.MaxLimit; } }
            public bool IsFullyRetracted
            { get { return MyPiston.Status == PistonStatus.Retracted && MyPiston.LowestPosition == MyPiston.MinLimit; } }
            public bool IsFullyExtendedAbsolute
            {
                get
                {
                    if (Direction)
                    { return IsFullyExtended; }
                    return IsFullyRetracted;
                }
            }

            public bool IsMoving
            {
                get
                {
                    var status = MyPiston.Status;
                    return status == PistonStatus.Extending || status == PistonStatus.Retracting;
                }
            }

            /// <summary>
            /// True = Extending.
            /// </summary>
            public bool Direction
            { get; private set; }

            public Piston(IMyPistonBase piston)
            {
                if (!piston.IsWorking)
                { throw new ArgumentException(string.Format("{0} is in an unfinished state!", piston.CustomName)); }
                MyPiston = piston;
                Activate(true);
            }

            /// <summary>
            /// Returns true if the piston is reset.
            /// </summary>
            /// <returns></returns>
            public bool Reset()
            {
                if (IsFullyRetracted)
                {
                    MyPiston.MinLimit = 0;
                    MyPiston.MaxLimit = 0;
                    Direction = true;
                    return true;
                }

                if (!IsMoving)
                { MoveOne(-MyPiston.HighestPosition, SPEED * -5); }

                return false;
            }
            public void Reverse()
            {
                Direction = !Direction;
            }

            public void MoveOne(float distance, float velocity)
            {
                MyPiston.MaxLimit += distance;
                MyPiston.MinLimit = MyPiston.MaxLimit;
                MyPiston.Velocity = velocity;
                Direction = velocity >= 0;
            }
            public void Activate(bool value)
            {
                if (value)
                { MyPiston.ApplyAction("OnOff_On"); }
                else
                { MyPiston.ApplyAction("OnOff_Off"); }
            }
        }
    }
}
