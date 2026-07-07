using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using Sts2ModTranslatorOpenCC.Ui;

namespace Sts2ModTranslatorOpenCC.Patches;

[HarmonyPatch(typeof(NMainMenuSubmenuStack), nameof(NMainMenuSubmenuStack.GetSubmenuType), typeof(Type))]
public static class TraditionalizeSubmenuPatch
{
    private static readonly ConditionalWeakTable<NMainMenuSubmenuStack, TraditionalizeSubmenu> Submenus = new();

    public static bool Prefix(NMainMenuSubmenuStack __instance, Type type, ref NSubmenu __result)
    {
        if (type != typeof(TraditionalizeSubmenu)) return true;

        __result = Submenus.GetValue(__instance, CreateSubmenu);
        return false;
    }

    private static TraditionalizeSubmenu CreateSubmenu(NMainMenuSubmenuStack stack)
    {
        var submenu = new TraditionalizeSubmenu { Visible = false };
        stack.AddChild(submenu);
        return submenu;
    }
}
