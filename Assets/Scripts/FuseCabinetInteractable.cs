using System;
using UnityEngine;

/// <summary>
/// Simulates locomotive fuse cabinet failures and manual replacement workflow.
/// </summary>
[DisallowMultipleComponent]
public class FuseCabinetInteractable : MonoBehaviour
{
    [Serializable]
    private class FuseSlot
    {
        public TrainPhysicsAdvanced.ElectricalCircuit circuit;
        public bool installed = true;
        public bool blown;
    }

    [Header("Fuse Inventory")]
    [SerializeField] private FuseSlot[] fuseSlots =
    {
        new FuseSlot { circuit = TrainPhysicsAdvanced.ElectricalCircuit.MainAlternator },
        new FuseSlot { circuit = TrainPhysicsAdvanced.ElectricalCircuit.FuelPump },
        new FuseSlot { circuit = TrainPhysicsAdvanced.ElectricalCircuit.Electronics },
        new FuseSlot { circuit = TrainPhysicsAdvanced.ElectricalCircuit.DynamicBrake }
    };
    [SerializeField, Min(0)] private int spareFuses = 8;

    [Header("Failure Thresholds")]
    [SerializeField, Min(1f)] private float engineOverheatFuseBlowTemp = 110f;
    [SerializeField, Range(0f, 1f)] private float tractionOverloadFuseBlowLoad = 0.95f;
    [SerializeField, Min(1f)] private float tractionOverheatFuseBlowTemp = 165f;

    [Header("Door")]
    [SerializeField] private bool cabinetDoorOpen;

    public bool CabinetDoorOpen => cabinetDoorOpen;
    public int SpareFuses => spareFuses;

    public event Action<TrainPhysicsAdvanced.ElectricalCircuit> FuseBlown;
    public event Action<TrainPhysicsAdvanced.ElectricalCircuit> FuseReplaced;

    private TrainPhysicsAdvanced _trainPhysics;
    private LocomotiveStats _locomotiveStats;

    private void Awake()
    {
        _trainPhysics = GetComponent<TrainPhysicsAdvanced>();
        _locomotiveStats = GetComponent<LocomotiveStats>();
        PushFuseStatesToPhysics();
    }

    private void Update()
    {
        if (_trainPhysics == null)
        {
            return;
        }

        if (_locomotiveStats != null && _locomotiveStats.EngineTemperature >= engineOverheatFuseBlowTemp)
        {
            BlowFuse(TrainPhysicsAdvanced.ElectricalCircuit.MainAlternator);
        }

        if (_trainPhysics.TractionMotorLoad01 >= tractionOverloadFuseBlowLoad)
        {
            BlowFuse(TrainPhysicsAdvanced.ElectricalCircuit.Electronics);
        }

        if (_trainPhysics.TractionMotorTemperatureC >= tractionOverheatFuseBlowTemp)
        {
            BlowFuse(TrainPhysicsAdvanced.ElectricalCircuit.FuelPump);
            BlowFuse(TrainPhysicsAdvanced.ElectricalCircuit.DynamicBrake);
        }
    }

    public void BindPhysics(TrainPhysicsAdvanced trainPhysics)
    {
        _trainPhysics = trainPhysics;
        PushFuseStatesToPhysics();
    }

    public void ToggleCabinetDoor()
    {
        cabinetDoorOpen = !cabinetDoorOpen;
    }

    public bool PullBlownFuse(TrainPhysicsAdvanced.ElectricalCircuit circuit)
    {
        if (!cabinetDoorOpen)
        {
            return false;
        }

        FuseSlot slot = GetSlot(circuit);
        if (slot == null || !slot.blown || !slot.installed)
        {
            return false;
        }

        slot.installed = false;
        UpdatePhysicsCircuit(circuit, false);
        return true;
    }

    public bool InsertFreshFuse(TrainPhysicsAdvanced.ElectricalCircuit circuit)
    {
        if (!cabinetDoorOpen || spareFuses <= 0)
        {
            return false;
        }

        FuseSlot slot = GetSlot(circuit);
        if (slot == null || slot.installed)
        {
            return false;
        }

        slot.installed = true;
        slot.blown = false;
        spareFuses--;
        UpdatePhysicsCircuit(circuit, true);
        FuseReplaced?.Invoke(circuit);
        return true;
    }

    public bool IsCircuitOperational(TrainPhysicsAdvanced.ElectricalCircuit circuit)
    {
        FuseSlot slot = GetSlot(circuit);
        return slot != null && slot.installed && !slot.blown;
    }

    private void BlowFuse(TrainPhysicsAdvanced.ElectricalCircuit circuit)
    {
        FuseSlot slot = GetSlot(circuit);
        if (slot == null || slot.blown || !slot.installed)
        {
            return;
        }

        slot.blown = true;
        UpdatePhysicsCircuit(circuit, false);
        FuseBlown?.Invoke(circuit);
    }

    private void PushFuseStatesToPhysics()
    {
        if (_trainPhysics == null || fuseSlots == null)
        {
            return;
        }

        for (int i = 0; i < fuseSlots.Length; i++)
        {
            FuseSlot slot = fuseSlots[i];
            _trainPhysics.SetCircuitOperational(slot.circuit, slot.installed && !slot.blown);
        }
    }

    private void UpdatePhysicsCircuit(TrainPhysicsAdvanced.ElectricalCircuit circuit, bool operational)
    {
        if (_trainPhysics != null)
        {
            _trainPhysics.SetCircuitOperational(circuit, operational);
        }
    }

    private FuseSlot GetSlot(TrainPhysicsAdvanced.ElectricalCircuit circuit)
    {
        if (fuseSlots == null)
        {
            return null;
        }

        for (int i = 0; i < fuseSlots.Length; i++)
        {
            if (fuseSlots[i].circuit == circuit)
            {
                return fuseSlots[i];
            }
        }

        return null;
    }
}
