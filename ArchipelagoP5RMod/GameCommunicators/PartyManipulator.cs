using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;

namespace ArchipelagoP5RMod.GameCommunicators;

public class PartyManipulator
{
    private readonly FlagManipulator _flagManipulator;
    private IHook<FlowFunctionWrapper.BasicFlowFunc> _partyAddHook;
    public const uint MAX_PARTY_SIZE = 4;

    private unsafe PartyMember* _currentPartyMembers; // ushort[4]

    private static readonly HashSet<PartyMember> _unlockedPartyMembers = [];

    public PartyManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks)
    {
        this._flagManipulator = flagManipulator;
        _unlockedPartyMembers.Add(PartyMember.Joker);

        AddressScanner.DelayedScanPattern(
            "48 83 EC 28 33 C9 E8 ?? ?? ?? ?? 4C 8D 05 ?? ?? ?? ?? 44 8B C8 49 8B C8",
            address =>
            {
                _partyAddHook =
                    hooks.CreateHook<FlowFunctionWrapper.BasicFlowFunc>(PartyAddFlowImpl, address).Activate();
                unsafe
                {
                    int disp = *(int*)(address + 11 + 3);
                    _currentPartyMembers = (PartyMember*)(address + 11 + 7 + disp);
                    MyLogger.DebugLog($"Dynamically scanned _currentPartyMembers: 0x{(IntPtr)_currentPartyMembers:X}");
                }
            });
    }

    public unsafe bool IsPartyMembersValid => _currentPartyMembers != null && (ulong)_currentPartyMembers >= 0x10000 && (ulong)_currentPartyMembers <= 0x7FFFFFFFFFFF;

    public unsafe uint CurrPartySize()
    {
        if (!IsPartyMembersValid) return 0;
        uint i;
        for (i = 0; i < MAX_PARTY_SIZE; i++)
        {
            if (_currentPartyMembers[i] == 0)
            {
                break;
            }
        }

        return i;
    }

    public unsafe bool PartyAdd(PartyMember partyMem)
    {
        if (!IsPartyMembersValid) return false;
        var iter = _currentPartyMembers;
        do
        {
            if (*iter == partyMem)
            {
                return false;
            }

            iter++;
        } while (iter < _currentPartyMembers + 4);

        int i = 0;
        iter = _currentPartyMembers;
        do
        {
            if (*iter == 0)
            {
                _currentPartyMembers[i] = partyMem;
                return true;
            }

            i++;
            iter++;
        } while (iter < _currentPartyMembers + 4);

        _currentPartyMembers[3] = partyMem;
        return true;
    }

    public bool UnlockPartyMember(PartyMember partyMember)
    {
        bool added = _unlockedPartyMembers.Add(partyMember);

        switch (partyMember)
        {
            case PartyMember.Skull:
                _flagManipulator.SetBit(11824, true); // Party
                _flagManipulator.SetBit(1168, true); // Group chat
                break;
            case PartyMember.Mona:
                _flagManipulator.SetBit(11825, true); // Party
                break;
            case PartyMember.Panther:
                _flagManipulator.SetBit(11826, true); // Party
                _flagManipulator.SetBit(1169, true); // Group Chat
                break;
            case PartyMember.Fox:
                _flagManipulator.SetBit(11827, true); // Party
                _flagManipulator.SetBit(1170, true); // Group Chat 
                break;
            case PartyMember.Queen:
                _flagManipulator.SetBit(11828, true); // Party
                _flagManipulator.SetBit(1171, true); // Group Chat
                break;
            case PartyMember.Noir:
                _flagManipulator.SetBit(11829, true); // Party
                _flagManipulator.SetBit(1173, true); // Group Chat
                break;
            case PartyMember.Oracle:
                _flagManipulator.SetBit(11830, true); // Party
                _flagManipulator.SetBit(1172, true); // Group Chat
                break;
            case PartyMember.Crow:
                _flagManipulator.SetBit(11831, true); // Party
                _flagManipulator.SetBit(1174, true); // Group Chat 
                break;
            case PartyMember.Violet:
                _flagManipulator.SetBit(11832, true); // Party
                _flagManipulator.SetBit(527, true); // Group Chat
                break;
        }

        if (CurrPartySize() < MAX_PARTY_SIZE)
        {
            PartyAdd(partyMember);
        }

        return added;
    }

    public void HandleApItem(object? sender, ApConnector.ApItemReceivedEvent? e)
    {
        if (e is null || e.Handled || e.ApItem.Type != ItemType.PartyMember ||
            _flagManipulator.CheckBit(FlagManipulator.SHOWING_MESSAGE))
            return;

        PartyMember partyMember = (PartyMember)e.ApItem.Id;
        bool added = UnlockPartyMember(partyMember);

        if (added)
        {
            _flagManipulator.SetCount(FlagManipulator.AP_CURR_REWARD_CMM_ABILITY, e.ApItem.Id);
            _flagManipulator.SetBit(FlagManipulator.SHOWING_MESSAGE, true);

            FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NotifyPartyMemberJoined);
        }

        e.Handled = true;
    }

    private long PartyAddFlowImpl()
    {
        PartyMember partyMember = (PartyMember)FlowFunctionWrapper.GetFlowscriptInt4Arg(0);

        if (partyMember == PartyMember.Joker || _unlockedPartyMembers.Contains(partyMember))
        {
            return _partyAddHook.OriginalFunction();
        }

        MyLogger.DebugLog($"[PARTY] Blocked adding party member {partyMember} (not unlocked yet).");
        return 1;
    }

    #region Save/Load

    public byte[] SaveData()
    {
        MemoryStream stream = new();

        stream.Write(ByteTools.CollectionToByteArray(_unlockedPartyMembers,
            mem => BitConverter.GetBytes((short)mem), PartyMember.None));

        return stream.ToArray();
    }

    public void LoadData(MemoryStream data)
    {
        var results = ByteTools.ByteArrayToCollection<HashSet<PartyMember>, PartyMember>(data, sizeof(PartyMember),
            b => (PartyMember)BitConverter.ToInt16(b), PartyMember.None);

        _unlockedPartyMembers.Clear();
        _unlockedPartyMembers.UnionWith(results);
    }

    #endregion
}