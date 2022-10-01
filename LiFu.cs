using System;
using System.Collections.Generic;
using System.Diagnostics;
using Num = System.Numerics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using Dalamud.Game;
using Dalamud.Plugin;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Hooking;
using LiFu;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.ClientState.Conditions;
using static Lifu.ClickManager;
using Dalamud.Game.Gui.Toast;
using System.ComponentModel;

namespace Lifu
{
    public unsafe class Lifu : IDalamudPlugin
    {
		public string Name => "Lifu";
        public static Lifu Plugin { get; private set; }
        //public static Configuration Config { get; private set; }
        private Configuration config;
        private DalamudPluginInterface pluginInterface;
        public GameObject 选中;
        private Hook<JiaoHook> jiaoHook;
        private delegate IntPtr JiaoHook(long a1, long a2);
        private IntPtr 接任务参数;
        private delegate IntPtr XuanHook(int a, Int16 b, int c,long d,byte e, long f);
        private Hook<XuanHook> xuanHook;
        private delegate IntPtr TiJiaoHook(long a, long b, int c, Int16 d, long e);
        private Hook<TiJiaoHook> tiJiaoHook;
        private delegate IntPtr HuoQuHook(IntPtr a);
        private Hook<HuoQuHook> huoQuHook;
        public long 提交参数1;
        public long 提交参数2;
        public bool 匹配;
        public IntPtr 理符任务;
        internal ClickManager clickManager;
        private static RaptureAtkUnitManager* raptureAtkUnitManager;
        private delegate IntPtr Ceshi(IntPtr arg1, EventType arg2, uint arg3, void* target, IntPtr arg5);
        private Hook<Ceshi> ceShi;
        private delegate void AccessGameObjDelegate(IntPtr g_ControlSystem_TargetSystem, IntPtr targte, char p3);
        private  AccessGameObjDelegate? accessGameObject;
        private IntPtr 理符;
        private delegate IntPtr RenWuDelegate(IntPtr a, uint targetId, IntPtr dataPtr);
        private Hook<RenWuDelegate> RenWuHook;


        public Lifu(DalamudPluginInterface pluginInterface)
        {

            this.pluginInterface = pluginInterface;
			DalamudApi.Initialize(this, pluginInterface);
			this.config = (((Configuration)this.pluginInterface.GetPluginConfig()) ?? new Configuration());
            this.config.Initialize();
            Int16* canshu = stackalloc Int16[0x20];
            接任务参数 = DalamudApi.SigScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 4C 8D 0D ?? ?? ?? ?? 45 84 ED"); 
            jiaoHook ??=Hook<JiaoHook>.FromAddress(DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B6 D8 EB 06"), new JiaoHook(JiaoDetour));
            jiaoHook.Enable();
            xuanHook ??= Hook<XuanHook>.FromAddress(DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? EB 10 48 8B 0D ?? ?? ?? ??"), new XuanHook(XuanDetour));
            xuanHook.Enable();
			tiJiaoHook ??=Hook<TiJiaoHook>.FromAddress(DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 4D A3 8B D8"), new TiJiaoHook(TiJiaoDetour));
			tiJiaoHook.Enable();
            huoQuHook ??=Hook<HuoQuHook>.FromAddress(DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D BB ?? ?? ?? ?? 33 D2 8D 4E 10"), new HuoQuHook(HuoQuDetour));
            huoQuHook.Enable();
            Enabled = false;
			this.accessGameObject = Marshal.GetDelegateForFunctionPointer<AccessGameObjDelegate>(DalamudApi.SigScanner.ScanText("E9 ?? ?? ?? ?? 48 8B 01 FF 50 08"));
			this.config.undiyici = false;
            理符 = DalamudApi.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? 48 83 C4 28 E9 ?? ?? ?? ?? 48 83 EC 28 33 D2") + 0xa268 + 0xa8 + 0x54;
            clickManager = new ClickManager(this);
            RenWuHook ??= Hook<RenWuDelegate>.FromAddress(DalamudApi.SigScanner.ScanText("40 55 56 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 8B ?? 49 8B ??"), new RenWuDelegate(RenWuDetour));
            RenWuHook.Enable();
            提交参数1 = (long)DalamudApi.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? 48 83 C4 28 E9 ?? ?? ?? ?? 8B 05 ?? ?? ?? ?? 89 05 ?? ?? ?? ?? C3 CC CC CC 8B 05 ?? ?? ?? ?? 89 05 ?? ?? ?? ?? C3 CC CC CC F3 0F 10 05 ?? ?? ?? ??");
            var 参数2跳转地址= DalamudApi.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 4C 8B 40 18 45 8B 40 18");
            var abc = Marshal.ReadIntPtr( Marshal.ReadIntPtr(参数2跳转地址)+0x70)-0x8+0x10180+0x70; ;
            提交参数2 = Marshal.ReadInt64( Marshal.ReadIntPtr((IntPtr)abc) + 0x5e8);
            DalamudApi.ChatGui.ChatMessage+= Chat_OnChatMessage;
            raptureAtkUnitManager = AtkStage.GetSingleton()->RaptureAtkUnitManager;
            DalamudApi.Framework.Update += Update;
        }

        private IntPtr RenWuDetour(IntPtr a, uint targetId, IntPtr dataPtr)
        {
            ushort[] abc =new ushort []{581,123,533,976,582,581,533,682 ,497, 581,533, 976 ,948,390};
            var opcode = (ushort)Marshal.ReadInt16(dataPtr, 0x2);
            if (!abc.Contains(opcode))
            {
                //Print(opcode.ToString());
            }


                return RenWuHook.Original(a, targetId, dataPtr);

           
        }

        private IntPtr HuoQuDetour(IntPtr a)
        {
            理符任务 = a + 0x54;
            return huoQuHook.Original(a);
        }
        public static AtkUnitBase* GetFocusedAddon()
        {
            var units = raptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            var count = units.Count;
            return count == 0 ? null : (&units.AtkUnitEntries)[count - 1];
        }
        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {

            if (type != (XivChatType) 62) return;
            string matched = message.ToString();
            var RegexStr = "获得了.*?金币";
            匹配 = Regex.IsMatch(matched, RegexStr);
            var a = HasLeve();
            var b = HasLiFu();
            if (匹配&&a && b&& this.config.undiyici)
            {
                //FFF();
            }
        }

        public void DOMo()
        {
            Task.Run(() =>
            {
                var Actors = DalamudApi.ObjectTable.Where(i => i.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc)
                    .Where(i => i.Name.ToString() == "阿尔德伊恩");
                foreach (var actor in Actors)
                {
                    选中 = actor;
                }
                accessGameObject(DalamudApi.TargetManager.Address, 选中.Address, (char)0);
                //Thread.Sleep(1000);
                //int* canshu = stackalloc int[6];
                //canshu[0] = 1489;
                //canshu[1] = 84607233;
                //byte abc = 0;
                //xuanHook.Original(0xE0000, 0x7, 0, (long)canshu, abc, 0);
                //Thread.Sleep(200);
            });
        }
        private IntPtr TiJiaoDetour(long a, long b, int c, short d, long e)
        {
            //if (提交参数1 == 0)
            //{
                
            //    提交参数1 = a;

            //}
            //if (提交参数2 == 0)
            //{
            //    提交参数2 = b;
            //}
            return  tiJiaoHook.Original(a,b,c,d,e);
        }
        private IntPtr XuanDetour(int a, Int16 b, int c, long d, byte e, long f)
        {

            return xuanHook.Original(a,b,c,d,e,f);
        }

        private IntPtr JiaoDetour(long a1, long a2)
        {
            return jiaoHook.Original(a1,a2);
        }



        public static void PrintEcho(string message) => DalamudApi.ChatGui.Print($"[Lifu] {message}");
        public static void PrintError(string message) => DalamudApi.ChatGui.PrintError($"[Lifu] {message}");

        private void Update(Framework framework)
        {
            var isMainMenu = !DalamudApi.Condition.Any();
            if (isMainMenu)
            {
                return;
            }
            理符完成();
            //Print(addonName.ToString());
            foreach (var item in 接任务)
            {
                var b = Marshal.ReadInt64(接任务参数);
                if (b > 0)
                {
                    jiaoHook.Original(b, 1635);
                    接任务.Clear();
                    break;
                }

                if (item.ToString() == DateTime.Now.ToString())
                {
                    接任务.Clear();
                    break;
                }
            }
            if (DalamudApi.Condition[ConditionFlag.OccupiedInQuestEvent] || DalamudApi.Condition[ConditionFlag.OccupiedInEvent])
            {
                
                if (Enabled)
                {
                    继续交货();
                    任务需求();
                    交易优质道具();
                    TickTalk();
                    TickQuestComplete();
                    退出任务();

				}
                

            }
        }



        void TickQuestComplete()
        {
            var addon = DalamudApi.GameGui.GetAddonByName("JournalResult", 1);
            if (addon == IntPtr.Zero)
            {
                return;
            }
            var questAddon = (AtkUnitBase*)addon;
            if (questAddon->UldManager.NodeListCount <= 4) return;
            var buttonNode = (AtkComponentNode*)questAddon->UldManager.NodeList[4];
            if (buttonNode->Component->UldManager.NodeListCount <= 2) return;
            var textComponent = (AtkTextNode*)buttonNode->Component->UldManager.NodeList[2];
            if ("完成" != Marshal.PtrToStringUTF8((IntPtr)textComponent->NodeText.StringPtr)) return;
            //pi.Framework.Gui.Chat.Print(Environment.TickCount + " Pass");
            if (!((AddonJournalResult*)addon)->CompleteButton->IsEnabled) return;
            clickManager.SendClickThrottled(addon, EventType.CHANGE, 1, ((AddonJournalResult*)addon)->CompleteButton->AtkComponentBase.OwnerNode);
        }
        void TickTalk()
        {
            var addon = DalamudApi.GameGui.GetAddonByName("Talk", 1);
            if (addon == IntPtr.Zero) return;
            var talkAddon = (AtkUnitBase*)addon;
            if (!talkAddon->IsVisible/* || !talkAddon->UldManager.NodeList[14]->IsVisible*/) return;
           
            var questAddon = (AtkUnitBase*)addon;
            var textComponent = (AtkComponentNode*)questAddon->UldManager.NodeList[20];
            var a = (AtkTextNode*)textComponent;

            if ("格里格" == Marshal.PtrToStringUTF8((IntPtr)a->NodeText.StringPtr))
            {
                var hasLiFu = HasLiFu();
                var abc = HasLeve();
                if (hasLiFu == true||abc)
                {
                    clickManager.SendClick(addon, ClickManager.EventType.MOUSE_CLICK, 0, ((AddonTalk*)talkAddon)->AtkEventListenerUnk.AtkStage);
                }
                else
                {
                    var b = Marshal.ReadInt64(接任务参数);
                    if (b > 0)
                    {
                        //1635为巨匠任务在leve.csv里面
                        jiaoHook.Original(b, 1635);
                    }
                }


            }
            else
            {
                clickManager.SendClick(addon, ClickManager.EventType.MOUSE_CLICK, 0, ((AddonTalk*)talkAddon)->AtkEventListenerUnk.AtkStage);
            }
            //var imageNode = (AtkImageNode*)talkAddon->UldManager.NodeList[14];
            //if (imageNode->PartsList->Parts[imageNode->PartId].U != 288) return;
            //clickManager.SendClick(addon, ClickManager.EventType.MOUSE_CLICK, 0, ((AddonTalk*)talkAddon)->AtkStage);
        }
        void 理符完成()
        {
            var addon = DalamudApi.GameGui.GetAddonByName("_Image", 1);
            if (addon == IntPtr.Zero) return;
            
            var selectStrAddon = (AtkUnitBase*)addon;
            if (!selectStrAddon->IsVisible)
            {
                //NextClick = Environment.TickCount + 500;
                return;
            }
            var a = (AtkImageNode*)selectStrAddon->UldManager.NodeList[1];
            var textureInfo = a->PartsList->Parts[a->PartId].UldAsset;
            var texType = textureInfo->AtkTexture.TextureType;
            if (texType == TextureType.Resource)
            {
                var texFileNameStdString = &textureInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
                var texString = texFileNameStdString->Length < 16
                    ? Marshal.PtrToStringAnsi((IntPtr)texFileNameStdString->Buffer)
                    : Marshal.PtrToStringAnsi((IntPtr)texFileNameStdString->BufferPtr);
                //Print(texString);
            }
        }
        void 继续交货()
        {
            var addon = DalamudApi.GameGui.GetAddonByName("SelectIconString", 1);
            if (addon == IntPtr.Zero) return;
            var selectStrAddon = (AtkUnitBase*)addon;
            if (!selectStrAddon->IsVisible)
            {
                //NextClick = Environment.TickCount + 500;
                return;
            }
            if (selectStrAddon->UldManager.NodeListCount <= 3) return;
            var a = (AtkComponentNode*)selectStrAddon->UldManager.NodeList[2];
			var d = (AtkComponentNode*)a->Component->UldManager.NodeList[1];
			var txt = (AtkTextNode*)d -> Component->UldManager.NodeList[4];
          
            if ("制作委托：提高品质用的炼金药" == Marshal.PtrToStringUTF8((IntPtr)txt->NodeText.StringPtr))
            {

                clickManager.SelectStringClick(addon, 0);
            }


        }
		void 退出任务()
		{
			var addon = DalamudApi.GameGui.GetAddonByName("SelectString", 1);
			if (addon == IntPtr.Zero) return;
			var selectStrAddon = (AtkUnitBase*)addon;
			if (!selectStrAddon->IsVisible)
			{
				//NextClick = Environment.TickCount + 500;
				return;
			}
			if (selectStrAddon->UldManager.NodeListCount <= 3) return;
			var a = (AtkComponentNode*)selectStrAddon->UldManager.NodeList[2];
			var txt = (AtkTextNode*)selectStrAddon->UldManager.NodeList[3];
			if ("有什么事？" == Marshal.PtrToStringUTF8((IntPtr)txt->NodeText.StringPtr))
			{
				clickManager.SelectStringClick(addon, 3);
			}

		}
		void 任务需求()
        {
            var addon = DalamudApi.GameGui.GetAddonByName("Request", 1);
            if (addon == IntPtr.Zero) return;
            var selectStrAddon = (AtkUnitBase*)addon;
            if (!selectStrAddon->IsVisible)
            {
                //NextClick = Environment.TickCount + 500;
                return;
            }
            if (selectStrAddon->UldManager.NodeListCount <= 3) return;
            var 图片= (AtkComponentNode*)selectStrAddon->UldManager.NodeList[16];
            var 没提交 = 图片->AtkResNode.IsVisible;
            var focusedAddon = GetFocusedAddon();
            var addonName = focusedAddon != null ? Marshal.PtrToStringAnsi((IntPtr)focusedAddon->Name) : string.Empty;
            if (没提交==true&&提交参数1!=0)
            {
                if (提交参数1==0)
                {
					var 参数2跳转地址 = DalamudApi.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 4C 8B 40 18 45 8B 40 18");
					var abc = Marshal.ReadIntPtr(Marshal.ReadIntPtr(参数2跳转地址) + 0x70) - 0x8 + 0x10180 + 0x70; ;
					提交参数2 = Marshal.ReadInt64(Marshal.ReadIntPtr((IntPtr)abc) + 0x5e8);
				}
                //2005为巨匠
				tiJiaoHook.Original(提交参数1, 提交参数2, 2005, 0, 1);
			}
            if (!没提交)
            {
                var questAddon = (AtkUnitBase*)addon;
                var buttonNode = (AtkComponentNode*)questAddon->UldManager.NodeList[4];
                if (buttonNode->Component->UldManager.NodeListCount <= 2) return;
                var textComponent = (AtkTextNode*)buttonNode->Component->UldManager.NodeList[2];
                var abc = Marshal.PtrToStringUTF8((IntPtr)textComponent->NodeText.StringPtr);
               
                if ("递交" != Marshal.PtrToStringUTF8((IntPtr)textComponent->NodeText.StringPtr)) return;
                var eventListener = (AtkEventListener*)addon;
                var receiveEventAddress = new IntPtr(eventListener->vfunc[2]);
                //clickManager.SendClickThrottled(addon, EventType.FOCUS_MAX, 2, buttonNode);
                if (addonName=="Request")
                {
                    clickManager.SendClickThrottled(addon, EventType.CHANGE, 0, buttonNode);
                }
                else
                {
                    clickManager.SendClickThrottled(addon, EventType.FOCUS_MAX, 2, buttonNode);
                }
            }
        }
        void 交易优质道具()
        {
            var addon = DalamudApi.GameGui.GetAddonByName("SelectYesno", 1);
            if (addon == IntPtr.Zero) return;
            var selectStrAddon = (AtkUnitBase*)addon;
            if (!selectStrAddon->IsVisible)
            {
                //NextClick = Environment.TickCount + 500;
                return;
            }
            if (selectStrAddon->UldManager.NodeListCount <= 6) return;
            var a = (AtkComponentNode*)selectStrAddon->UldManager.NodeList[11];
            var txt = (AtkTextNode*)selectStrAddon->UldManager.NodeList[15];
            if ("确定要交易优质道具吗？" != Marshal.PtrToStringUTF8((IntPtr)txt->NodeText.StringPtr)) return;

            if (a->Component->UldManager.NodeListCount <= 2) return;
            var b = (AtkTextNode*)a->Component->UldManager.NodeList[2];
            //if (b->Component->UldManager.NodeListCount <= 3) return;

            //var c = (AtkTextNode*)b->Component->UldManager.NodeList[2]
            if ("确定" != Marshal.PtrToStringUTF8((IntPtr)b->NodeText.StringPtr)) return;
            clickManager.SendClick(addon, EventType.CHANGE, 0, ((AddonSelectYesno*)addon)->YesButton->AtkComponentBase.OwnerNode);
        }




        [Command("/Lifu")]
        [HelpMessage("简化理符")]
		public void LifuCommand(string command, string args)
		{
            string[] array = args.Split(new char[]
                {
                    ' '
                });
            string a = array[0];
            switch (a)
            {
                case "aer":
                    DOMo();
                    break;
                case "xuan":
                    tiJiaoHook.Original(提交参数1, 提交参数2, 2005, 0, 1);
                    break;
                case "call":
					交易优质道具();

					break;
                case "ge":
                    Task.Run(() =>
                    {
                        var Actors = DalamudApi.ObjectTable.Where(i => i.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc)
                            .Where(i => i.Name.ToString() == "格里格");
                        foreach (var actor in Actors)
                        {
                            选中 = actor;
                        }
                        accessGameObject(DalamudApi.TargetManager.Address, 选中.Address, (char)0);
                        MouseDo.SendKeycode((uint)VirtualKey.ESCAPE);
                    });
                    break;
                case "null":
                    Task.Run(() =>
                    {
                        Thread.Sleep(10);
                        MouseDo.SendKeycode((uint)VirtualKey.ESCAPE);
                    });
                    break;
                case "kai":
                    Enabled = !Enabled;
                    DalamudApi.Toasts.ShowQuest("理符 " + (Enabled ? "开启" : "关闭"),
               new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
                    break;
                default:
                    break;
            }
        }
        public void JieRenWu()
        {
            var time1 = DateTime.Now;
            var Time = time1.AddSeconds(5);
            接任务.Add(Time);
        }
        public static List<DateTime> 接任务 = new List<DateTime>();
        private bool Enabled;

        public SeString ReadSeString(byte* ptr)
        {
            var offset = 0;
            while (true)
            {
                var b = *(ptr + offset);
                if (b == 0)
                {
                    break;
                }
                offset += 1;
            }
            var bytes = new byte[offset];
            Marshal.Copy(new IntPtr(ptr), bytes, 0, offset);
            return SeString.Parse(bytes);
        }

        bool HasLeve()
        {
            var holder = ((UIModule*)DalamudApi.GameGui.GetUIModule())->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
            
            var size = holder.StringArrays[22]->AtkArrayData.Size;
            var array = holder.StringArrays[22]->StringArray;
            bool has = false;
            for (var i = 0; i < size; i++)
            {
                if (array[i] == null) continue;
                var seString = ReadSeString(array[i]).TextValue;
                if (seString.Contains("将巨匠药酒提交给阿尔德伊恩")) has = true;
            }
            
            return has;
        }
        bool HasLiFu()
        {
            bool have =false;
            if (理符任务!=IntPtr.Zero)
            {
                for (int i = 0; i < 10; i++)
                {
                    var offset = 理符 + 36 * i;
                    var lifu = Marshal.ReadInt32(offset);
                    if (lifu == 1635)
                    {
                       return true;
                    }
                    else
                    {
                        have = false;
                    }
                }
            }

            return have;
        }

        public void Print(string a)
        { DalamudApi.ChatGui.Print(a); }

		#region IDisposable Support
		protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            jiaoHook.Disable();
            xuanHook.Disable();
            tiJiaoHook.Disable();
            huoQuHook.Disable();
            RenWuHook.Disable();
            //this.config.Save();
            this.pluginInterface.SavePluginConfig(this.config);
			DalamudApi.Framework.Update -= Update;
            DalamudApi.ChatGui.ChatMessage -= Chat_OnChatMessage;

            DalamudApi.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
