using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimTalk.Broadcast;

public class BroadcastDialogueWindow : Window
{
    private readonly Pawn _initiator;
    private readonly Pawn _origin;
    private string _text = "";
    private const string TextFieldControlName = "BroadcastTalkTextField";

    public BroadcastDialogueWindow(Pawn initiator, Pawn origin)
    {
        _initiator = initiator;
        _origin = origin;
        doCloseX = true;
        draggable = true;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
    }

    public override Vector2 InitialSize => new(420f, 150f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;

        string labelText = _initiator.IsPlayer()
            ? "RimTalk.Broadcast.WhatToSayAsPlayer".Translate(_origin.LabelShortCap)
            : "RimTalk.Broadcast.WhatToSayAsPawn".Translate(_initiator.LabelShortCap, _origin.LabelShortCap);

        Widgets.Label(new Rect(0f, 0f, inRect.width, 25f), labelText);

        GUI.SetNextControlName(TextFieldControlName);
        _text = Widgets.TextField(new Rect(0f, 30f, inRect.width, 35f), _text);

        if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
            GUI.FocusControl(TextFieldControlName);

        if (GUI.GetNameOfFocusedControl() == TextFieldControlName &&
            Event.current.isKey &&
            Event.current.keyCode == KeyCode.Return)
        {
            TrySend();
            Close();
            Event.current.Use();
        }

        if (Widgets.ButtonText(new Rect(0f, 75f, inRect.width / 2f - 5f, 35f),
                "RimTalk.Broadcast.Send".Translate()))
        {
            TrySend();
            Close();
        }

        if (Widgets.ButtonText(new Rect(inRect.width / 2f + 5f, 75f, inRect.width / 2f - 5f, 35f),
                "RimTalk.FloatMenu.Cancel".Translate()))
        {
            Close();
        }
    }

    private void TrySend()
    {
        if (string.IsNullOrWhiteSpace(_text)) return;

        // 玩家广播：直接发（CanTalk 对玩家恒 true）:contentReference[oaicite:11]{index=11}
        if (_initiator.IsPlayer() || CustomDialogueService.CanTalk(_initiator, _origin))
        {
            BroadcastDialogueService.ExecuteBroadcast(_initiator, _origin, _text);
            return;
        }

        // pawn 广播：走过去后再广播（add-on 的 pending）
        BroadcastPending.Pending[_initiator] = new BroadcastPending.PendingBroadcast(_origin, _text);

        Job job = JobMaker.MakeJob(JobDefOf.Goto, _origin);
        job.playerForced = true;
        job.collideWithPawns = false;
        job.locomotionUrgency = LocomotionUrgency.Jog;

        _initiator.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }
}
