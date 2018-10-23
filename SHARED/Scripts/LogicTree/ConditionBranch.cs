﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharedTools_Stuff;
using PlayerAndEditorGUI;

namespace STD_Logic {
    public class ConditionBranch : AbstractKeepUnrecognized_STD, IGotName, IPEGI, IAmConditional, IcanBeDefault_STD, IPEGI_ListInspect
    {

        public enum ConditionBranchType { OR, AND }

        public List<ConditionLogic> conds = new List<ConditionLogic>();
        public List<ConditionBranch> branches = new List<ConditionBranch>();

        public ConditionBranchType type;
        public string description = "new branch";
        public TaggedTarget targ;

        Values TargetValues => targ.TryGetValues(Values.global);
        
        public string NameForPEGI
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        #region Encode & Decode
        public bool isDefault => conds.Count == 0;

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfNotEmpty("wb", branches)
            .Add_IfNotEmpty("v", conds)
            .Add_ifNotZero("t", (int)type)
            .Add_IfNotEmpty("d", description)
            .Add("tag", targ)
            .Add_IfNotNegative("insB", browsedBranch)
            .Add_IfNotNegative("ic", browsedCondition);

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "t": type = (ConditionBranchType)data.ToInt(); break;
                case "d": description = data; break;
                case "tag": data.DecodeInto(out targ); break;
                case "wb": data.DecodeInto_List(out branches); break;
                case "v": data.DecodeInto_List(out conds); break;
                case "insB": browsedBranch = data.ToInt(); break;
                case "ic": browsedCondition = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        public bool IsTrue => CheckConditions(TargetValues);

        public bool CheckConditions(Values vals) {
            vals = targ.TryGetValues(vals);

            switch (type)
            {
                case ConditionBranchType.AND:
                    foreach (var c in conds)
                        if (c.TestFor(vals) == false) return false;
                    foreach (var b in branches)
                        if (b.CheckConditions(vals) == false) return false;
                    return true;
                case ConditionBranchType.OR:
                    foreach (var c in conds)
                        if (c.TestFor(vals) == true)
                            return true;
                    foreach (var b in branches)
                        if (b.CheckConditions(vals) == true) return true;
                    return ((conds.Count == 0) && (branches.Count == 0));
            }
            return true;
        }

        public void ForceToTrue(Values vals)
        {

            vals = targ.TryGetValues(vals);

            switch (type)
            {
                case ConditionBranchType.AND:
                    foreach (var c in conds)
                        c.ForceConditionTrue(vals);
                    foreach (var b in branches)
                        b.ForceToTrue(vals);
                    break;
                case ConditionBranchType.OR:
                    if (conds.Count > 0)
                    {

                        conds[0].ForceConditionTrue(vals);
                        return;
                    }
                    if (branches.Count > 0)
                    {
                        branches[0].ForceToTrue(vals);
                        return;
                    }
                    break;
            }

        }

        #region Inspector
        int browsedBranch = -1;
        int browsedCondition = -1;

#if PEGI
        public override bool Inspect() {

            var tmpVals = Values.global;
            tmpVals = targ.TryGetValues(tmpVals);

            bool changed = false;

            if (browsedBranch == -1) {
                if (pegi.Click(type + (type == ConditionBranchType.AND ? " (ALL should be true)" : " (At least one should be true)"),
                    (type == ConditionBranchType.AND ? "All conditions and sub branches should be true" : "At least one condition or sub branch should be true")))
                    type = (type == ConditionBranchType.AND ? ConditionBranchType.OR : ConditionBranchType.AND);

                (CheckConditions(tmpVals) ? icon.Active : icon.InActive).write();

                changed |= conds.edit_List(ref browsedCondition);
            }

            pegi.Line(Color.black);

            changed |= "Sub Branches".edit_List(branches, ref browsedBranch);


            pegi.newLine();

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            (IsTrue ? icon.Active : icon.InActive).write();

            var changed = this.inspect_Name();

            if (icon.Enter.Click())
                edited = ind;

            return changed;
        }



#endif
        #endregion

    }


    public interface IAmConditional {
        bool CheckConditions(Values vals);
    }

    public static class ConditionalsExtensions {
    
        public static bool Test_And_For(this List<IAmConditional> lst, Values vals) {

        if (lst == null)
            return true;

            foreach (var e in lst) 
            if (e != null && !e.CheckConditions(vals))
                return false;


            return true;
        }

    }


}

