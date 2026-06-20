using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MistDemoBootstrap : MonoBehaviour
{
    private enum RosterZone
    {
        Party,
        Reserve
    }

    private class RosterDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        public MistDemoBootstrap Owner;
        public RosterZone Zone;
        public int Index;
        private bool wasDragged;

        public void OnBeginDrag(PointerEventData eventData)
        {
            wasDragged = true;
            if (Owner != null)
            {
                Owner.BeginRosterDrag(this);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Owner != null)
            {
                Owner.UpdateRosterDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Owner != null)
            {
                Owner.EndRosterDrag();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (Owner != null)
            {
                Owner.DropRosterOn(this);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!wasDragged && Owner != null)
            {
                Owner.ShowRosterDetail(Zone, Index);
            }
            wasDragged = false;
        }
    }

    private enum DemoEventType
    {
        None,
        Check,
        Battle,
        Loot
    }

    private class Investigator
    {
        public string Name;
        public string Job;
        public int Str;
        public int Dex;
        public int Con;
        public int Int;
        public int Cha;
        public int Luck;
        public int Hp;
        public int MaxHp;
        public int San;
        public int MaxSan;
        public string PortraitKey;
    }

    private class SceneNode
    {
        public string Id;
        public string Title;
        public string Body;
        public string North;
        public string South;
        public string West;
        public string East;
        public DemoEventType EventType;
        public string EventTitle;
        public string EventBody;
        public string CheckAttribute;
        public int Difficulty;
        public bool Completed;
    }

    private class Enemy
    {
        public string Name;
        public int Hp;
        public int Dex;
    }

    private Canvas canvas;
    private RectTransform root;
    private RectTransform sceneCard;
    private RectTransform eventPanel;
    private RectTransform resultPanel;
    private RectTransform agencyPanel;
    private Text locationText;
    private Text cashText;
    private Text debtText;
    private Text sceneTitleText;
    private Text sceneBodyText;
    private Text directionsText;
    private Text eventTitleText;
    private Text eventBodyText;
    private Text resultText;
    private Text partyText;
    private Text battleLogText;
    private Image sceneArtwork;
    private Image eventArtwork;
    private Image phoneIcon;
    private Image backpackIcon;
    private Image mapIcon;
    private Image settingsIcon;
    private Image[] portraitImages;
    private RectTransform[] hpFillRects;
    private RectTransform[] sanFillRects;
    private Text[] investigatorNameTexts;
    private Text[] hpValueTexts;
    private Text[] sanValueTexts;
    private RectTransform backpackPanel;
    private RectTransform mapPanel;
    private RectTransform recruitPanel;
    private RectTransform teamEditPanel;
    private RectTransform rosterDetailPanel;
    private RectTransform shopPanel;
    private Text backpackContentText;
    private Text mapContentText;
    private Text agencyStatusText;
    private Text agencyTeamText;
    private Text recruitNameText;
    private Text recruitStatsText;
    private Text teamEditContentText;
    private Text shopContentText;
    private Text rosterDetailText;
    private Image recruitPortraitImage;
    private Image rosterDetailPortrait;
    private Image rosterDragGhost;
    private RosterDragItem activeRosterDrag;

    private Dictionary<string, SceneNode> nodes = new Dictionary<string, SceneNode>();
    private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private Dictionary<string, string> nodeSpriteKeys = new Dictionary<string, string>();
    private List<Investigator> party = new List<Investigator>();
    private List<Investigator> reserve = new List<Investigator>();
    private List<string> inventory = new List<string>();
    private List<string> portraitKeys = new List<string>();
    private SceneNode currentNode;
    private Investigator recruitCandidate;
    private string recruitPortraitKey = "portrait_detective";
    private Enemy activeEnemy;
    private bool sceneLocked;
    private bool inBattle;
    private int cash = 120;
    private int debt;
    private int runIndex;
    private string activeRunName = "";
    private string activeEnemyName = "剥皮者";
    private Vector2 dragStart;
    private System.Random random = new System.Random();
    private const string AssetFolder = @"C:\Users\zyuh\Desktop\迷雾档案";
    private const int MaxReserveCount = 8;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        if (FindObjectOfType<MistDemoBootstrap>() != null)
        {
            return;
        }

        GameObject host = new GameObject("Mist Demo Bootstrap");
        host.AddComponent<MistDemoBootstrap>();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        LoadExternalSprites();
        BuildData();
        BuildUi();
        MoveTo("gate");
        ShowAgency("欢迎回到特情处。请整备队伍后开始调查。");
    }

    private void Update()
    {
        if (sceneLocked)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 dragEnd = Input.mousePosition;
            Vector2 delta = dragEnd - dragStart;
            if (delta.magnitude < 80f)
            {
                return;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                TryMove(delta.x > 0f ? "east" : "west");
            }
            else
            {
                TryMove(delta.y > 0f ? "north" : "south");
            }
        }
    }

    private void BuildData()
    {
        party.Add(new Investigator { Name = "梁探长", Job = "侦探", Str = 8, Dex = 9, Con = 10, Int = 15, Cha = 11, Luck = 10, Hp = 18, MaxHp = 20, San = 85, MaxSan = 100, PortraitKey = "portrait_detective" });
        party.Add(new Investigator { Name = "霍尔特", Job = "打手", Str = 14, Dex = 13, Con = 13, Int = 8, Cha = 7, Luck = 9, Hp = 12, MaxHp = 15, San = 45, MaxSan = 100, PortraitKey = "portrait_rogue" });
        party.Add(new Investigator { Name = "艾琳", Job = "学者", Str = 7, Dex = 11, Con = 9, Int = 12, Cha = 15, Luck = 12, Hp = 8, MaxHp = 10, San = 95, MaxSan = 100, PortraitKey = "portrait_scholar" });
        reserve.Add(new Investigator { Name = "米勒", Job = "警察", Str = 12, Dex = 10, Con = 12, Int = 9, Cha = 8, Luck = 10, Hp = 14, MaxHp = 14, San = 68, MaxSan = 100, PortraitKey = "portrait_police" });
        reserve.Add(new Investigator { Name = "罗伊", Job = "小偷", Str = 8, Dex = 15, Con = 9, Int = 11, Cha = 10, Luck = 14, Hp = 10, MaxHp = 10, San = 72, MaxSan = 100, PortraitKey = "portrait_thief" });

        AddNode("gate", "黑水镇锯木厂入口", "雨水顺着铁门流下，锈蚀的厂牌只剩半截。远处传来木料摩擦般的尖响。", "hall", "", "", "", DemoEventType.None, "", "", "", 1);
        AddNode("hall", "锯木厂大厅", "巨大的锯片悬在梁上，地面有拖拽过的血迹。空气里混着霉味和海盐味。", "office", "gate", "generator", "coldroom", DemoEventType.Check, "血迹调查", "血迹并非一路拖向出口，而是绕过锯台后突然中断。需要进行智力检定。", "INT", 1);
        AddNode("generator", "发电机房", "老式发电机沉在黑暗里，电闸旁有一截烧焦的手套。", "", "", "", "hall", DemoEventType.Check, "启动发电机", "你可以强行扳动电闸。失败会让队伍受到轻微伤害。", "STR", 1);
        AddNode("coldroom", "冷藏仓库", "厚重的冷气从门缝里涌出，墙上挂着不该属于木材厂的铁钩。", "", "", "hall", "", DemoEventType.Battle, "剥皮者的祭坛", "一个披着湿皮革的影子从货架后站起。战斗开始。", "", 1);
        AddNode("office", "厂长办公室", "桌上的账本被水泡皱，抽屉里露出一张写着码头编号的纸片。", "", "hall", "", "", DemoEventType.Loot, "厂长的暗格", "你找到一份线索档案和少量现金。", "", 1);

        nodeSpriteKeys["gate"] = "scene_gate";
        nodeSpriteKeys["hall"] = "scene_path";
        nodeSpriteKeys["generator"] = "scene_cabin";
        nodeSpriteKeys["coldroom"] = "scene_bloodkin";
        nodeSpriteKeys["office"] = "scene_rest";

        GenerateInvestigation();
    }

    private void GenerateInvestigation()
    {
        runIndex++;
        nodes.Clear();
        nodeSpriteKeys.Clear();

        string[] runNames = { "黑水溪森林", "旧锯木厂夜巡", "猎人小屋失踪案", "腐化血亲专案" };
        string[] enemies = { "被腐化的血亲", "剥皮者", "林地潜伏者", "无脸伐木工" };
        string[] spriteKeys = { "scene_gate", "scene_path", "scene_cabin", "scene_bloodkin", "scene_rest", "scene_ritual" };
        string[] checkAttrs = { "INT", "STR", "DEX", "CHA", "LUCK" };

        activeRunName = Pick(runNames) + " #" + runIndex;
        activeEnemyName = Pick(enemies);

        AddNode("gate",
            activeRunName,
            Pick(new string[] {
                "特情处把你们送到调查边界。雾很低，树影像被水泡开的墨迹。",
                "临时路障后只剩一盏煤油灯。档案袋上的火漆还没有完全冷却。",
                "调查从一条潮湿的小径开始。远处传来木板受压的吱呀声。"
            }),
            "hall", "", "", "", DemoEventType.None, "", "", "", 1);

        AddNode("hall",
            Pick(new string[] { "林间小道", "锯木厂外场", "雾中岔路" }),
            Pick(new string[] {
                "地上有新鲜脚印，但每隔几步就会突然消失。",
                "腐烂木屑堆成一条线，像是有人故意留下的路标。",
                "树皮上刻着重复的符号，方向感在这里变得不可靠。"
            }),
            "office", "gate", "generator", "coldroom", RandomEvent(), "现场检定",
            Pick(new string[] {
                "你需要判断哪条痕迹是真的，哪条只是诱饵。",
                "这里残留着一处异常线索，需要立刻处理。",
                "队伍必须快速完成检定，否则雾会吞掉退路。"
            }),
            Pick(checkAttrs), random.Next(1, 3));

        AddNode("generator",
            Pick(new string[] { "猎人小屋", "废弃补给点", "倒塌木棚" }),
            Pick(new string[] {
                "屋内还有余温，桌上的杯子却积满灰。",
                "补给箱被撬开，里面夹着半张潮湿地图。",
                "梁柱间挂着旧铃铛，风停时它仍会轻响。"
            }),
            "", "", "", "hall", RandomEvent(), "临时处置",
            Pick(new string[] {
                "这里可能藏有补给，也可能藏着陷阱。",
                "你可以强行搜索，但动静会引来东西。",
                "一个简易机关挡住了里面的抽屉。"
            }),
            Pick(checkAttrs), random.Next(1, 3));

        AddNode("coldroom",
            Pick(new string[] { "腐化祭坛", "血亲巢穴", "黑水溪浅滩" }),
            Pick(new string[] {
                "潮湿的石头围成半圆，中间摆着不属于人类的骨片。",
                "空气里的血腥味很新，脚下的泥浆在轻轻冒泡。",
                "雾从水面倒卷上来，像有什么东西正在呼吸。"
            }),
            "", "", "hall", "", DemoEventType.Battle, activeEnemyName,
            "敌意已经成形。队伍必须战斗，或立刻呼叫撤离。", "", 1);

        AddNode("office",
            Pick(new string[] { "可以休整的空地", "旧档案箱", "临时藏身处" }),
            Pick(new string[] {
                "这里暂时安全，灰烬里压着一枚带编号的铜片。",
                "箱子里塞满受潮文件，其中几页仍能辨认。",
                "篝火刚熄不久，旁边留着可带回特情处的证物。"
            }),
            "", "hall", "", "", DemoEventType.Loot, "线索收束",
            "你找到可以带回特情处的证据和少量现金。", "", 1);

        nodeSpriteKeys["gate"] = Pick(spriteKeys);
        nodeSpriteKeys["hall"] = Pick(spriteKeys);
        nodeSpriteKeys["generator"] = Pick(spriteKeys);
        nodeSpriteKeys["coldroom"] = "scene_bloodkin";
        nodeSpriteKeys["office"] = Pick(spriteKeys);
    }

    private DemoEventType RandomEvent()
    {
        int roll = random.Next(0, 100);
        if (roll < 45) return DemoEventType.Check;
        if (roll < 75) return DemoEventType.Loot;
        return DemoEventType.Battle;
    }

    private string Pick(string[] values)
    {
        return values[random.Next(0, values.Length)];
    }

    private void LoadExternalSprites()
    {
        sprites["ui_phone"] = LoadSprite(Path.Combine(AssetFolder, @"UI\电话.png"));
        sprites["ui_bag"] = LoadSprite(Path.Combine(AssetFolder, @"UI\背包.png"));
        sprites["ui_map"] = LoadSprite(Path.Combine(AssetFolder, @"UI\地图.png"));
        sprites["ui_settings"] = LoadSprite(Path.Combine(AssetFolder, @"UI\设置.png"));
        sprites["portrait_detective"] = LoadSprite(Path.Combine(AssetFolder, @"人物\侦探.png"));
        sprites["portrait_scholar"] = LoadSprite(Path.Combine(AssetFolder, @"人物\学者.png"));
        sprites["portrait_thief"] = LoadSprite(Path.Combine(AssetFolder, @"人物\小偷.png"));
        sprites["portrait_explorer"] = LoadSprite(Path.Combine(AssetFolder, @"人物\探险家.png"));
        sprites["portrait_rogue"] = LoadSprite(Path.Combine(AssetFolder, @"人物\流氓.png"));
        sprites["portrait_police"] = LoadSprite(Path.Combine(AssetFolder, @"人物\警察.png"));
        sprites["portrait_reporter"] = LoadSprite(Path.Combine(AssetFolder, @"人物\记者.png"));
        portraitKeys.Clear();
        portraitKeys.Add("portrait_detective");
        portraitKeys.Add("portrait_scholar");
        portraitKeys.Add("portrait_thief");
        portraitKeys.Add("portrait_explorer");
        portraitKeys.Add("portrait_rogue");
        portraitKeys.Add("portrait_police");
        portraitKeys.Add("portrait_reporter");
        sprites["scene_gate"] = LoadSprite(Path.Combine(AssetFolder, @"卡面\树林入口.png"));
        sprites["scene_path"] = LoadSprite(Path.Combine(AssetFolder, @"卡面\林间小道.png"));
        sprites["scene_cabin"] = LoadSprite(Path.Combine(AssetFolder, @"卡面\猎人小屋.png"));
        sprites["scene_bloodkin"] = LoadSprite(Path.Combine(AssetFolder, @"卡面\被腐化的血亲.png"));
        sprites["scene_rest"] = LoadSprite(Path.Combine(AssetFolder, @"卡面\可以休整的空地.png"));
        sprites["scene_ritual"] = LoadSprite(Path.Combine(AssetFolder, @"卡面\树林中废弃的五角星法阵.png"));
    }

    private Sprite LoadSprite(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        if (!texture.LoadImage(bytes))
        {
            return null;
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private void AddNode(string id, string title, string body, string north, string south, string west, string east, DemoEventType eventType, string eventTitle, string eventBody, string checkAttribute, int difficulty)
    {
        SceneNode node = new SceneNode();
        node.Id = id;
        node.Title = title;
        node.Body = body;
        node.North = north;
        node.South = south;
        node.West = west;
        node.East = east;
        node.EventType = eventType;
        node.EventTitle = eventTitle;
        node.EventBody = eventBody;
        node.CheckAttribute = checkAttribute;
        node.Difficulty = difficulty;
        nodes[id] = node;
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Mist Demo Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640f, 1170f);
        scaler.matchWidthOrHeight = 1f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRoot = canvasObject.GetComponent<RectTransform>();
        Image stageBackground = CreateImage("Outer Background", canvasRoot, new Color(0.03f, 0.03f, 0.028f, 1f));
        Stretch(stageBackground.rectTransform);

        Image phoneFrame = CreateImage("Phone 1x2 Frame", canvasRoot, new Color(0.08f, 0.075f, 0.065f, 1f));
        root = phoneFrame.rectTransform;
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(640f, 1170f);
        root.anchoredPosition = Vector2.zero;

        Image background = CreateImage("Background", root, new Color(0.08f, 0.075f, 0.065f, 1f));
        Stretch(background.rectTransform);

        RectTransform topHud = CreatePanel("Top HUD", root, new Color(0.12f, 0.105f, 0.09f, 0.96f));
        Anchor(topHud, 0f, 1f, 1f, 1f, 0f, -62f, 0f, 0f);
        locationText = CreateText("Location", topHud, "CASE: MISSING HEIR", 22, TextAnchor.MiddleLeft, new Color(0.95f, 0.87f, 0.68f, 1f));
        Anchor(locationText.rectTransform, 0.08f, 0f, 0.68f, 1f, 10f, 0f, -10f, 0f);
        cashText = CreateText("Cash", topHud, "$0", 22, TextAnchor.MiddleRight, new Color(0.95f, 0.87f, 0.68f, 1f));
        Anchor(cashText.rectTransform, 0.62f, 0f, 0.82f, 1f, 0f, 0f, -20f, 0f);
        debtText = CreateText("Debt", topHud, "", 12, TextAnchor.MiddleRight, new Color(0.9f, 0.45f, 0.38f, 1f));
        Anchor(debtText.rectTransform, 0.78f, 0f, 0.96f, 1f, 0f, 0f, -20f, 0f);
        Button extractButton = CreateButton("Telephone Extract", topHud, "", 24, new Color(0.32f, 0.1f, 0.09f, 1f));
        Anchor(extractButton.GetComponent<RectTransform>(), 0.86f, 0.16f, 0.98f, 0.88f, 0f, 0f, 0f, 0f);
        extractButton.onClick.AddListener(ShowExtraction);
        phoneIcon = CreateImage("Phone Icon", extractButton.transform, Color.white);
        phoneIcon.sprite = GetSprite("ui_phone");
        phoneIcon.preserveAspect = true;
        Anchor(phoneIcon.rectTransform, 0.12f, 0.12f, 0.88f, 0.88f, 0f, 0f, 0f, 0f);

        sceneCard = CreatePanel("Scene Card", root, new Color(0.73f, 0.66f, 0.5f, 1f));
        Anchor(sceneCard, 0.04f, 0.155f, 0.96f, 0.93f, 0f, 0f, 0f, 0f);
        Image sceneInner = CreateImage("Scene Inner", sceneCard, new Color(0.16f, 0.14f, 0.115f, 1f));
        Anchor(sceneInner.rectTransform, 0.025f, 0.025f, 0.975f, 0.975f, 0f, 0f, 0f, 0f);
        sceneArtwork = CreateImage("Scene Artwork", sceneCard, Color.white);
        Anchor(sceneArtwork.rectTransform, 0.045f, 0.34f, 0.955f, 0.965f, 0f, 0f, 0f, 0f);
        sceneArtwork.preserveAspect = true;
        Image textPlate = CreateImage("Scene Text Plate", sceneCard, new Color(0.06f, 0.045f, 0.035f, 0.82f));
        Anchor(textPlate.rectTransform, 0.045f, 0.065f, 0.955f, 0.365f, 0f, 0f, 0f, 0f);
        sceneTitleText = CreateText("Scene Title", sceneCard, "", 20, TextAnchor.UpperLeft, new Color(0.96f, 0.84f, 0.57f, 1f));
        Anchor(sceneTitleText.rectTransform, 0.07f, 0.28f, 0.93f, 0.36f, 0f, 0f, 0f, 0f);
        sceneBodyText = CreateText("Scene Body", sceneCard, "", 15, TextAnchor.UpperLeft, new Color(0.93f, 0.89f, 0.77f, 1f));
        Anchor(sceneBodyText.rectTransform, 0.07f, 0.12f, 0.93f, 0.28f, 0f, 0f, 0f, 0f);
        directionsText = CreateText("Directions", sceneCard, "", 14, TextAnchor.LowerCenter, new Color(0.7f, 0.63f, 0.47f, 1f));
        Anchor(directionsText.rectTransform, 0.05f, 0.008f, 0.95f, 0.06f, 0f, 0f, 0f, 0f);

        RectTransform bottomHud = CreatePanel("Bottom HUD", root, new Color(0.1f, 0.09f, 0.08f, 0.97f));
        Anchor(bottomHud, 0f, 0f, 1f, 0.235f, 0f, 0f, 0f, 0f);
        Button backpackButton = CreateButton("Backpack Button", bottomHud, "", 12, new Color(0.18f, 0.14f, 0.1f, 0.1f));
        Anchor(backpackButton.GetComponent<RectTransform>(), 0.72f, 0.04f, 0.91f, 0.28f, 0f, 0f, 0f, 0f);
        backpackButton.onClick.AddListener(ShowBackpack);
        backpackIcon = CreateImage("Backpack Icon", backpackButton.transform, Color.white);
        backpackIcon.sprite = GetSprite("ui_bag");
        backpackIcon.preserveAspect = true;
        Stretch(backpackIcon.rectTransform);
        Button mapButton = CreateButton("Map Button", bottomHud, "", 12, new Color(0.18f, 0.14f, 0.1f, 0.1f));
        Anchor(mapButton.GetComponent<RectTransform>(), 0.09f, 0.04f, 0.28f, 0.28f, 0f, 0f, 0f, 0f);
        mapButton.onClick.AddListener(ShowMap);
        mapIcon = CreateImage("Map Icon", mapButton.transform, Color.white);
        mapIcon.sprite = GetSprite("ui_map");
        mapIcon.preserveAspect = true;
        Stretch(mapIcon.rectTransform);
        settingsIcon = CreateImage("Settings Icon", topHud, Color.white);
        settingsIcon.sprite = GetSprite("ui_settings");
        settingsIcon.preserveAspect = true;
        Anchor(settingsIcon.rectTransform, 0.01f, 0.2f, 0.08f, 0.8f, 0f, 0f, 0f, 0f);
        portraitImages = new Image[3];
        hpFillRects = new RectTransform[3];
        sanFillRects = new RectTransform[3];
        investigatorNameTexts = new Text[3];
        hpValueTexts = new Text[3];
        sanValueTexts = new Text[3];
        for (int i = 0; i < portraitImages.Length; i++)
        {
            RectTransform panel = CreatePanel("Investigator Panel " + i, bottomHud, new Color(0.23f, 0.19f, 0.13f, 0.12f));
            float x0 = 0.06f + i * 0.31f;
            Anchor(panel, x0, 0.31f, x0 + 0.26f, 0.96f, 0f, 0f, 0f, 0f);

            Image portraitBack = CreateImage("Portrait Back " + i, panel, new Color(0.95f, 0.89f, 0.72f, 1f));
            Anchor(portraitBack.rectTransform, 0.21f, 0.48f, 0.79f, 1f, 0f, 0f, 0f, 0f);
            portraitImages[i] = CreateImage("Portrait " + i, portraitBack.transform, Color.white);
            portraitImages[i].preserveAspect = true;
            Stretch(portraitImages[i].rectTransform);

            investigatorNameTexts[i] = CreateText("Investigator Name " + i, panel, "", 16, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.7f, 1f));
            Anchor(investigatorNameTexts[i].rectTransform, 0f, 0.34f, 1f, 0.48f, 0f, 0f, 0f, 0f);

            Text hpLabel = CreateText("HP Label " + i, panel, "HP", 13, TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.7f, 1f));
            Anchor(hpLabel.rectTransform, 0.02f, 0.19f, 0.26f, 0.31f, 0f, 0f, 0f, 0f);
            RectTransform hpBar = CreatePanel("HP Bar " + i, panel, new Color(0.16f, 0.08f, 0.06f, 1f));
            Anchor(hpBar, 0.25f, 0.2f, 0.98f, 0.3f, 0f, 0f, 0f, 0f);
            Image hpFill = CreateImage("HP Fill " + i, hpBar, new Color(0.84f, 0.24f, 0.22f, 1f));
            hpFillRects[i] = hpFill.rectTransform;
            Stretch(hpFillRects[i]);
            hpValueTexts[i] = CreateText("HP Value " + i, hpBar, "", 12, TextAnchor.MiddleCenter, Color.white);
            Stretch(hpValueTexts[i].rectTransform);

            Text sanLabel = CreateText("SAN Label " + i, panel, "SAN", 13, TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.7f, 1f));
            Anchor(sanLabel.rectTransform, 0.02f, 0.06f, 0.26f, 0.18f, 0f, 0f, 0f, 0f);
            RectTransform sanBar = CreatePanel("SAN Bar " + i, panel, new Color(0.08f, 0.13f, 0.07f, 1f));
            Anchor(sanBar, 0.25f, 0.07f, 0.98f, 0.17f, 0f, 0f, 0f, 0f);
            Image sanFill = CreateImage("SAN Fill " + i, sanBar, new Color(0.3f, 0.74f, 0.34f, 1f));
            sanFillRects[i] = sanFill.rectTransform;
            Stretch(sanFillRects[i]);
            sanValueTexts[i] = CreateText("SAN Value " + i, sanBar, "", 12, TextAnchor.MiddleCenter, Color.white);
            Stretch(sanValueTexts[i].rectTransform);
        }
        RefreshPortraitSlots();
        partyText = CreateText("Party", bottomHud, "", 13, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.7f, 1f));
        Anchor(partyText.rectTransform, 0.3f, 0.04f, 0.7f, 0.26f, 0f, 0f, 0f, 0f);

        eventPanel = CreatePanel("Event Layer", root, new Color(0f, 0f, 0f, 0.58f));
        Stretch(eventPanel);
        RectTransform eventCard = CreatePanel("Event Card", eventPanel, new Color(0.82f, 0.75f, 0.58f, 1f));
        Anchor(eventCard, 0.08f, 0.22f, 0.92f, 0.82f, 0f, 0f, 0f, 0f);
        eventArtwork = CreateImage("Event Artwork", eventCard, Color.white);
        Anchor(eventArtwork.rectTransform, 0.07f, 0.42f, 0.93f, 0.92f, 0f, 0f, 0f, 0f);
        eventArtwork.preserveAspect = true;
        Image eventTextPlate = CreateImage("Event Text Plate", eventCard, new Color(0.94f, 0.87f, 0.68f, 0.92f));
        Anchor(eventTextPlate.rectTransform, 0.06f, 0.18f, 0.94f, 0.45f, 0f, 0f, 0f, 0f);
        eventTitleText = CreateText("Event Title", eventCard, "", 24, TextAnchor.UpperLeft, new Color(0.12f, 0.08f, 0.05f, 1f));
        Anchor(eventTitleText.rectTransform, 0.08f, 0.34f, 0.92f, 0.45f, 0f, 0f, 0f, 0f);
        eventBodyText = CreateText("Event Body", eventCard, "", 15, TextAnchor.UpperLeft, new Color(0.16f, 0.11f, 0.07f, 1f));
        Anchor(eventBodyText.rectTransform, 0.08f, 0.23f, 0.92f, 0.34f, 0f, 0f, 0f, 0f);
        battleLogText = CreateText("Battle Log", eventCard, "", 13, TextAnchor.UpperLeft, new Color(0.26f, 0.12f, 0.09f, 1f));
        Anchor(battleLogText.rectTransform, 0.08f, 0.17f, 0.92f, 0.23f, 0f, 0f, 0f, 0f);

        Button primaryButton = CreateButton("Primary Action", eventCard, "行动", 18, new Color(0.24f, 0.15f, 0.1f, 1f));
        Anchor(primaryButton.GetComponent<RectTransform>(), 0.08f, 0.06f, 0.45f, 0.17f, 0f, 0f, 0f, 0f);
        primaryButton.onClick.AddListener(ResolveEvent);
        Button closeButton = CreateButton("Continue", eventCard, "继续", 18, new Color(0.28f, 0.24f, 0.19f, 1f));
        Anchor(closeButton.GetComponent<RectTransform>(), 0.55f, 0.06f, 0.92f, 0.17f, 0f, 0f, 0f, 0f);
        closeButton.onClick.AddListener(CloseEvent);

        resultPanel = CreatePanel("Toast", root, new Color(0.08f, 0.06f, 0.05f, 0.92f));
        Anchor(resultPanel, 0.08f, 0.84f, 0.92f, 0.94f, 0f, 0f, 0f, 0f);
        resultText = CreateText("Toast Text", resultPanel, "", 15, TextAnchor.MiddleCenter, Color.white);
        Stretch(resultText.rectTransform);

        BuildBackpackPanel();
        BuildMapPanel();
        BuildAgencyPanel();

        eventPanel.gameObject.SetActive(false);
        resultPanel.gameObject.SetActive(false);
        backpackPanel.gameObject.SetActive(false);
        mapPanel.gameObject.SetActive(false);
        agencyPanel.gameObject.SetActive(false);
        UpdateHud();
    }

    private void BuildBackpackPanel()
    {
        backpackPanel = CreatePanel("Backpack Panel", root, new Color(0f, 0f, 0f, 0.62f));
        Stretch(backpackPanel);
        RectTransform card = CreatePanel("Backpack Card", backpackPanel, new Color(0.78f, 0.69f, 0.5f, 1f));
        Anchor(card, 0.05f, 0.12f, 0.95f, 0.86f, 0f, 0f, 0f, 0f);
        Text title = CreateText("Backpack Title", card, "调查背包", 28, TextAnchor.UpperLeft, new Color(0.13f, 0.08f, 0.04f, 1f));
        Anchor(title.rectTransform, 0.07f, 0.87f, 0.93f, 0.96f, 0f, 0f, 0f, 0f);
        backpackContentText = CreateText("Backpack Content", card, "", 18, TextAnchor.UpperLeft, new Color(0.16f, 0.1f, 0.05f, 1f));
        Anchor(backpackContentText.rectTransform, 0.07f, 0.16f, 0.93f, 0.86f, 0f, 0f, 0f, 0f);
        Button close = CreateButton("Close Backpack", card, "关闭", 20, new Color(0.24f, 0.15f, 0.1f, 1f));
        Anchor(close.GetComponent<RectTransform>(), 0.32f, 0.055f, 0.68f, 0.125f, 0f, 0f, 0f, 0f);
        close.onClick.AddListener(HideBackpack);
    }

    private void BuildMapPanel()
    {
        mapPanel = CreatePanel("Map Panel", root, new Color(0f, 0f, 0f, 0.62f));
        Stretch(mapPanel);
        RectTransform card = CreatePanel("Map Card", mapPanel, new Color(0.12f, 0.1f, 0.08f, 1f));
        Anchor(card, 0.05f, 0.11f, 0.95f, 0.87f, 0f, 0f, 0f, 0f);
        Text title = CreateText("Map Title", card, "调查地图", 28, TextAnchor.UpperLeft, new Color(0.92f, 0.8f, 0.55f, 1f));
        Anchor(title.rectTransform, 0.07f, 0.88f, 0.93f, 0.96f, 0f, 0f, 0f, 0f);
        mapContentText = CreateText("Map Content", card, "", 17, TextAnchor.UpperLeft, new Color(0.87f, 0.82f, 0.7f, 1f));
        Anchor(mapContentText.rectTransform, 0.07f, 0.15f, 0.93f, 0.86f, 0f, 0f, 0f, 0f);
        Button close = CreateButton("Close Map", card, "关闭", 20, new Color(0.24f, 0.15f, 0.1f, 1f));
        Anchor(close.GetComponent<RectTransform>(), 0.32f, 0.055f, 0.68f, 0.125f, 0f, 0f, 0f, 0f);
        close.onClick.AddListener(HideMap);
    }

    private void BuildAgencyPanel()
    {
        agencyPanel = CreatePanel("Agency Panel", root, new Color(0.07f, 0.06f, 0.05f, 1f));
        Stretch(agencyPanel);

        Image header = CreateImage("Agency Header", agencyPanel, new Color(0.12f, 0.105f, 0.09f, 1f));
        Anchor(header.rectTransform, 0f, 0.9f, 1f, 1f, 0f, 0f, 0f, 0f);
        Text title = CreateText("Agency Title", header.transform, "特情处", 30, TextAnchor.MiddleLeft, new Color(0.96f, 0.84f, 0.57f, 1f));
        Anchor(title.rectTransform, 0.07f, 0f, 0.58f, 1f, 0f, 0f, 0f, 0f);
        Text subTitle = CreateText("Agency Subtitle", header.transform, "Case Files: The Mist", 13, TextAnchor.MiddleRight, new Color(0.72f, 0.66f, 0.52f, 1f));
        Anchor(subTitle.rectTransform, 0.5f, 0f, 0.93f, 1f, 0f, 0f, 0f, 0f);

        Image notice = CreateImage("Agency Notice", agencyPanel, new Color(0.18f, 0.145f, 0.1f, 1f));
        Anchor(notice.rectTransform, 0.07f, 0.73f, 0.93f, 0.86f, 0f, 0f, 0f, 0f);
        Text noticeText = CreateText("Agency Notice Text", notice.transform, "撤离报告已归档。调查员无永久损伤，但资源损失会保留。", 17, TextAnchor.MiddleLeft, new Color(0.9f, 0.83f, 0.66f, 1f));
        Anchor(noticeText.rectTransform, 0.06f, 0f, 0.94f, 1f, 0f, 0f, 0f, 0f);

        Image statusCard = CreateImage("Agency Status Card", agencyPanel, new Color(0.77f, 0.68f, 0.49f, 1f));
        Anchor(statusCard.rectTransform, 0.07f, 0.54f, 0.93f, 0.7f, 0f, 0f, 0f, 0f);
        agencyStatusText = CreateText("Agency Status Text", statusCard.transform, "", 18, TextAnchor.MiddleLeft, new Color(0.12f, 0.08f, 0.04f, 1f));
        Anchor(agencyStatusText.rectTransform, 0.06f, 0f, 0.94f, 1f, 0f, 0f, 0f, 0f);

        Image teamCard = CreateImage("Agency Team Card", agencyPanel, new Color(0.13f, 0.11f, 0.09f, 1f));
        Anchor(teamCard.rectTransform, 0.07f, 0.25f, 0.93f, 0.5f, 0f, 0f, 0f, 0f);
        agencyTeamText = CreateText("Agency Team Text", teamCard.transform, "", 16, TextAnchor.UpperLeft, new Color(0.9f, 0.85f, 0.72f, 1f));
        Anchor(agencyTeamText.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f, 0f, 0f, 0f, 0f);

        Button startButton = CreateButton("Agency Start", agencyPanel, "开始调查", 22, new Color(0.35f, 0.12f, 0.09f, 1f));
        Anchor(startButton.GetComponent<RectTransform>(), 0.12f, 0.145f, 0.88f, 0.205f, 0f, 0f, 0f, 0f);
        startButton.onClick.AddListener(StartInvestigationFromAgency);

        Button recruitButton = CreateButton("Agency Recruit", agencyPanel, "招募调查员", 18, new Color(0.27f, 0.19f, 0.11f, 1f));
        Anchor(recruitButton.GetComponent<RectTransform>(), 0.12f, 0.092f, 0.47f, 0.14f, 0f, 0f, 0f, 0f);
        recruitButton.onClick.AddListener(ShowRecruit);

        Button teamButton = CreateButton("Agency Team Edit", agencyPanel, "编辑队伍", 18, new Color(0.27f, 0.19f, 0.11f, 1f));
        Anchor(teamButton.GetComponent<RectTransform>(), 0.53f, 0.092f, 0.88f, 0.14f, 0f, 0f, 0f, 0f);
        teamButton.onClick.AddListener(ShowTeamEdit);

        Button shopButton = CreateButton("Agency Shop", agencyPanel, "商店", 18, new Color(0.24f, 0.17f, 0.1f, 1f));
        Anchor(shopButton.GetComponent<RectTransform>(), 0.12f, 0.035f, 0.32f, 0.08f, 0f, 0f, 0f, 0f);
        shopButton.onClick.AddListener(ShowShop);

        Button bagButton = CreateButton("Agency Bag", agencyPanel, "背包", 18, new Color(0.2f, 0.16f, 0.12f, 1f));
        Anchor(bagButton.GetComponent<RectTransform>(), 0.4f, 0.035f, 0.6f, 0.08f, 0f, 0f, 0f, 0f);
        bagButton.onClick.AddListener(ShowBackpack);

        Button mapButton = CreateButton("Agency Map", agencyPanel, "地图", 18, new Color(0.2f, 0.16f, 0.12f, 1f));
        Anchor(mapButton.GetComponent<RectTransform>(), 0.68f, 0.035f, 0.88f, 0.08f, 0f, 0f, 0f, 0f);
        mapButton.onClick.AddListener(ShowMap);

        BuildShopPanel();
        BuildRecruitPanel();
        BuildTeamEditPanel();
    }

    private void BuildShopPanel()
    {
        shopPanel = CreatePanel("Shop Panel", root, new Color(0f, 0f, 0f, 0.65f));
        Stretch(shopPanel);
        RectTransform card = CreatePanel("Shop Card", shopPanel, new Color(0.78f, 0.69f, 0.5f, 1f));
        Anchor(card, 0.08f, 0.13f, 0.92f, 0.86f, 0f, 0f, 0f, 0f);

        Text title = CreateText("Shop Title", card, "特情处商店", 30, TextAnchor.UpperLeft, new Color(0.12f, 0.08f, 0.04f, 1f));
        Anchor(title.rectTransform, 0.07f, 0.89f, 0.93f, 0.97f, 0f, 0f, 0f, 0f);

        shopContentText = CreateText("Shop Content", card, "", 17, TextAnchor.UpperLeft, new Color(0.12f, 0.08f, 0.04f, 1f));
        Anchor(shopContentText.rectTransform, 0.08f, 0.58f, 0.92f, 0.86f, 0f, 0f, 0f, 0f);

        Button healButton = CreateButton("Shop Heal", card, "急救治疗 $30", 18, new Color(0.24f, 0.15f, 0.1f, 1f));
        Anchor(healButton.GetComponent<RectTransform>(), 0.1f, 0.46f, 0.9f, 0.53f, 0f, 0f, 0f, 0f);
        healButton.onClick.AddListener(BuyHeal);

        Button sanButton = CreateButton("Shop San", card, "心理疏导 $40", 18, new Color(0.24f, 0.15f, 0.1f, 1f));
        Anchor(sanButton.GetComponent<RectTransform>(), 0.1f, 0.36f, 0.9f, 0.43f, 0f, 0f, 0f, 0f);
        sanButton.onClick.AddListener(BuySanCare);

        Button insuranceButton = CreateButton("Shop Insurance", card, "购买保险单 $60", 18, new Color(0.24f, 0.15f, 0.1f, 1f));
        Anchor(insuranceButton.GetComponent<RectTransform>(), 0.1f, 0.26f, 0.9f, 0.33f, 0f, 0f, 0f, 0f);
        insuranceButton.onClick.AddListener(BuyInsurance);

        Button close = CreateButton("Close Shop", card, "关闭", 18, new Color(0.2f, 0.16f, 0.12f, 1f));
        Anchor(close.GetComponent<RectTransform>(), 0.32f, 0.07f, 0.68f, 0.13f, 0f, 0f, 0f, 0f);
        close.onClick.AddListener(HideShop);

        shopPanel.gameObject.SetActive(false);
    }

    private void BuildTeamEditPanel()
    {
        teamEditPanel = CreatePanel("Team Edit Panel", root, new Color(0f, 0f, 0f, 0.65f));
        Stretch(teamEditPanel);
        RectTransform card = CreatePanel("Team Edit Card", teamEditPanel, new Color(0.78f, 0.69f, 0.5f, 1f));
        Anchor(card, 0.07f, 0.12f, 0.93f, 0.86f, 0f, 0f, 0f, 0f);

        Text title = CreateText("Team Edit Title", card, "队伍编辑", 30, TextAnchor.UpperLeft, new Color(0.12f, 0.08f, 0.04f, 1f));
        Anchor(title.rectTransform, 0.07f, 0.9f, 0.93f, 0.97f, 0f, 0f, 0f, 0f);
        teamEditContentText = CreateText("Team Edit Content", card, "", 16, TextAnchor.UpperLeft, new Color(0.12f, 0.08f, 0.04f, 1f));
        Anchor(teamEditContentText.rectTransform, 0.07f, 0.12f, 0.93f, 0.88f, 0f, 0f, 0f, 0f);

        Button close = CreateButton("Close Team Edit", card, "关闭", 17, new Color(0.2f, 0.16f, 0.12f, 1f));
        Anchor(close.GetComponent<RectTransform>(), 0.32f, 0.025f, 0.68f, 0.08f, 0f, 0f, 0f, 0f);
        close.onClick.AddListener(HideTeamEdit);

        BuildRosterDetailPanel();
        teamEditPanel.gameObject.SetActive(false);
    }

    private void BuildRosterDetailPanel()
    {
        rosterDetailPanel = CreatePanel("Roster Detail Panel", root, new Color(0f, 0f, 0f, 0.68f));
        Stretch(rosterDetailPanel);
        RectTransform card = CreatePanel("Roster Detail Card", rosterDetailPanel, new Color(0.78f, 0.69f, 0.5f, 1f));
        Anchor(card, 0.1f, 0.16f, 0.9f, 0.84f, 0f, 0f, 0f, 0f);

        Text title = CreateText("Roster Detail Title", card, "调查员档案", 28, TextAnchor.UpperLeft, new Color(0.12f, 0.08f, 0.04f, 1f));
        Anchor(title.rectTransform, 0.08f, 0.9f, 0.92f, 0.97f, 0f, 0f, 0f, 0f);

        Image portraitBack = CreateImage("Roster Detail Portrait Back", card, new Color(0.95f, 0.89f, 0.72f, 1f));
        Anchor(portraitBack.rectTransform, 0.34f, 0.61f, 0.66f, 0.88f, 0f, 0f, 0f, 0f);
        rosterDetailPortrait = CreateImage("Roster Detail Portrait", portraitBack.transform, Color.white);
        rosterDetailPortrait.preserveAspect = true;
        Stretch(rosterDetailPortrait.rectTransform);

        rosterDetailText = CreateText("Roster Detail Text", card, "", 18, TextAnchor.UpperLeft, new Color(0.12f, 0.08f, 0.04f, 1f));
        Anchor(rosterDetailText.rectTransform, 0.1f, 0.18f, 0.9f, 0.58f, 0f, 0f, 0f, 0f);

        Button close = CreateButton("Close Roster Detail", card, "关闭", 18, new Color(0.2f, 0.16f, 0.12f, 1f));
        Anchor(close.GetComponent<RectTransform>(), 0.32f, 0.07f, 0.68f, 0.13f, 0f, 0f, 0f, 0f);
        close.onClick.AddListener(HideRosterDetail);

        rosterDetailPanel.gameObject.SetActive(false);
    }

    private void BuildRecruitPanel()
    {
        recruitPanel = CreatePanel("Recruit Panel", root, new Color(0f, 0f, 0f, 0.65f));
        Stretch(recruitPanel);
        RectTransform card = CreatePanel("Recruit Card", recruitPanel, new Color(0.86f, 0.78f, 0.58f, 1f));
        Anchor(card, 0.08f, 0.13f, 0.92f, 0.86f, 0f, 0f, 0f, 0f);

        Text title = CreateText("Recruit Title", card, "招募档案", 30, TextAnchor.UpperLeft, new Color(0.08f, 0.055f, 0.035f, 1f));
        Anchor(title.rectTransform, 0.07f, 0.89f, 0.93f, 0.97f, 0f, 0f, 0f, 0f);

        Image portraitBack = CreateImage("Recruit Portrait Back", card, new Color(0.96f, 0.9f, 0.72f, 1f));
        Anchor(portraitBack.rectTransform, 0.31f, 0.56f, 0.69f, 0.88f, 0f, 0f, 0f, 0f);
        recruitPortraitImage = CreateImage("Recruit Portrait", portraitBack.transform, Color.white);
        recruitPortraitImage.preserveAspect = true;
        Stretch(recruitPortraitImage.rectTransform);

        recruitNameText = CreateText("Recruit Name", card, "", 24, TextAnchor.MiddleCenter, new Color(0.08f, 0.055f, 0.035f, 1f));
        Anchor(recruitNameText.rectTransform, 0.07f, 0.48f, 0.93f, 0.56f, 0f, 0f, 0f, 0f);
        recruitStatsText = CreateText("Recruit Stats", card, "", 17, TextAnchor.UpperLeft, new Color(0.12f, 0.08f, 0.04f, 1f));
        Anchor(recruitStatsText.rectTransform, 0.1f, 0.23f, 0.9f, 0.48f, 0f, 0f, 0f, 0f);

        Button rollButton = CreateButton("Roll Recruit", card, "刷新档案", 18, new Color(0.24f, 0.15f, 0.1f, 1f));
        Anchor(rollButton.GetComponent<RectTransform>(), 0.1f, 0.13f, 0.47f, 0.19f, 0f, 0f, 0f, 0f);
        rollButton.onClick.AddListener(RollRecruitCandidate);

        Button hireButton = CreateButton("Hire Recruit", card, "录用", 18, new Color(0.35f, 0.12f, 0.09f, 1f));
        Anchor(hireButton.GetComponent<RectTransform>(), 0.53f, 0.13f, 0.9f, 0.19f, 0f, 0f, 0f, 0f);
        hireButton.onClick.AddListener(HireRecruitCandidate);

        Button closeButton = CreateButton("Close Recruit", card, "关闭", 18, new Color(0.2f, 0.16f, 0.12f, 1f));
        Anchor(closeButton.GetComponent<RectTransform>(), 0.32f, 0.045f, 0.68f, 0.1f, 0f, 0f, 0f, 0f);
        closeButton.onClick.AddListener(HideRecruit);

        recruitPanel.gameObject.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void MoveTo(string id)
    {
        currentNode = nodes[id];
        if (sceneArtwork != null && nodeSpriteKeys.ContainsKey(id))
        {
            sceneArtwork.sprite = GetSprite(nodeSpriteKeys[id]);
        }
        sceneTitleText.text = currentNode.Title;
        sceneBodyText.text = currentNode.Body;
        locationText.text = currentNode.Title;
        directionsText.text = BuildDirections(currentNode);
        UpdateHud();

        if (!currentNode.Completed && currentNode.EventType != DemoEventType.None)
        {
            ShowCurrentEvent();
        }
    }

    private string BuildDirections(SceneNode node)
    {
        List<string> exits = new List<string>();
        if (!String.IsNullOrEmpty(node.North)) exits.Add("上滑: 北");
        if (!String.IsNullOrEmpty(node.South)) exits.Add("下滑: 南");
        if (!String.IsNullOrEmpty(node.West)) exits.Add("左滑: 西");
        if (!String.IsNullOrEmpty(node.East)) exits.Add("右滑: 东");
        return exits.Count == 0 ? "暂无出口" : String.Join("   ", exits.ToArray());
    }

    private void TryMove(string direction)
    {
        string target = "";
        if (direction == "north") target = currentNode.North;
        if (direction == "south") target = currentNode.South;
        if (direction == "west") target = currentNode.West;
        if (direction == "east") target = currentNode.East;

        if (String.IsNullOrEmpty(target))
        {
            Toast("这边没有路。");
            return;
        }

        StartCoroutine(FlipTo(target));
    }

    private System.Collections.IEnumerator FlipTo(string target)
    {
        float time = 0f;
        Vector3 original = sceneCard.localScale;
        while (time < 0.12f)
        {
            time += Time.deltaTime;
            sceneCard.localScale = new Vector3(Mathf.Lerp(1f, 0.9f, time / 0.12f), Mathf.Lerp(1f, 1.03f, time / 0.12f), 1f);
            yield return null;
        }

        MoveTo(target);

        time = 0f;
        while (time < 0.12f)
        {
            time += Time.deltaTime;
            sceneCard.localScale = Vector3.Lerp(sceneCard.localScale, original, time / 0.12f);
            yield return null;
        }
        sceneCard.localScale = original;
    }

    private void ShowCurrentEvent()
    {
        sceneLocked = true;
        inBattle = false;
        activeEnemy = null;
        eventPanel.gameObject.SetActive(true);
        eventTitleText.text = currentNode.EventTitle;
        eventBodyText.text = currentNode.EventBody;
        eventArtwork.sprite = currentNode.EventType == DemoEventType.Battle ? GetSprite("scene_bloodkin") : GetSprite("scene_ritual");
        battleLogText.text = "";

        if (currentNode.EventType == DemoEventType.Battle)
        {
            inBattle = true;
            eventTitleText.text = activeEnemyName;
            activeEnemy = new Enemy { Name = activeEnemyName, Hp = random.Next(12, 21), Dex = random.Next(8, 14) };
            battleLogText.text = "敌人 HP: " + activeEnemy.Hp + "\n点击行动进行一轮攻击。";
        }
    }

    private void ResolveEvent()
    {
        if (currentNode.EventType == DemoEventType.Check)
        {
            ResolveCheck();
        }
        else if (currentNode.EventType == DemoEventType.Battle)
        {
            ResolveBattleRound();
        }
        else if (currentNode.EventType == DemoEventType.Loot)
        {
            int reward = random.Next(25, 76);
            cash += reward;
            inventory.Add(Pick(new string[] { "潮湿档案", "铜制编号牌", "异常皮革", "黑水样本", "破损地图页" }));
            currentNode.Completed = true;
            Toast("获得 $" + reward + " 与调查物证。");
            CloseEvent();
        }
    }

    private void ResolveCheck()
    {
        Investigator actor = BestInvestigator(currentNode.CheckAttribute);
        int attribute = GetAttribute(actor, currentNode.CheckAttribute);
        int chance = attribute * 5;
        if (currentNode.Difficulty == 2) chance /= 2;
        if (currentNode.Difficulty == 3) chance /= 5;
        int roll = random.Next(1, 101);
        bool success = roll <= chance;

        if (success)
        {
            currentNode.Completed = true;
            cash += 20;
            Toast(actor.Name + " 检定成功: " + roll + "/" + chance + "，获得 $20。");
            CloseEvent();
        }
        else
        {
            party[0].Hp = Mathf.Max(1, party[0].Hp - 1);
            party[0].San = Mathf.Max(0, party[0].San - random.Next(3, 9));
            Toast(actor.Name + " 检定失败: " + roll + "/" + chance + "，队伍受到轻伤。");
            currentNode.Completed = true;
            CloseEvent();
        }
    }

    private void ResolveBattleRound()
    {
        if (!inBattle || activeEnemy == null)
        {
            return;
        }

        Investigator attacker = party[1];
        int chance = attacker.Dex * 5;
        int roll = random.Next(1, 101);
        string log = "";
        if (roll <= chance)
        {
            int damage = random.Next(1, 9);
            if (roll <= 5)
            {
                damage = Mathf.CeilToInt(damage * 1.5f);
            }
            activeEnemy.Hp -= damage;
            log += attacker.Name + " 命中，造成 " + damage + " 伤害。\n";
        }
        else
        {
            log += attacker.Name + " 攻击落空。\n";
        }

        if (activeEnemy.Hp <= 0)
        {
            currentNode.Completed = true;
            int reward = random.Next(45, 91);
            cash += reward;
            inventory.Add(activeEnemy.Name + "样本");
            battleLogText.text = log + activeEnemy.Name + "倒下。获得 $" + reward + " 与样本。";
            Toast("战斗胜利。");
            inBattle = false;
            return;
        }

        Investigator target = party[random.Next(0, party.Count)];
        int enemyDamage = random.Next(1, 5);
        target.Hp = Mathf.Max(1, target.Hp - enemyDamage);
        target.San = Mathf.Max(0, target.San - random.Next(2, 7));
        log += activeEnemy.Name + " 反击，" + target.Name + " 受到 " + enemyDamage + " 伤害。\n敌人 HP: " + activeEnemy.Hp;
        battleLogText.text = log;
        UpdateHud();
    }

    private Investigator BestInvestigator(string attribute)
    {
        Investigator best = party[0];
        int bestValue = GetAttribute(best, attribute);
        for (int i = 1; i < party.Count; i++)
        {
            int value = GetAttribute(party[i], attribute);
            if (value > bestValue)
            {
                best = party[i];
                bestValue = value;
            }
        }
        return best;
    }

    private int GetAttribute(Investigator investigator, string attribute)
    {
        if (attribute == "STR") return investigator.Str;
        if (attribute == "DEX") return investigator.Dex;
        if (attribute == "CON") return investigator.Con;
        if (attribute == "INT") return investigator.Int;
        if (attribute == "CHA") return investigator.Cha;
        if (attribute == "LUCK") return investigator.Luck;
        return investigator.Int;
    }

    private void ShowExtraction()
    {
        eventPanel.gameObject.SetActive(true);
        sceneLocked = true;
        eventArtwork.sprite = GetSprite("ui_phone");
        eventTitleText.text = "紧急撤离";
        eventBodyText.text = "电话亭传来刺耳铃声。确认撤离会结束本次探索，并随机承受资源代价。";
        battleLogText.text = "再次点击行动确认撤离。";
        Button[] buttons = eventPanel.GetComponentsInChildren<Button>();
        if (buttons.Length > 0)
        {
            buttons[0].onClick.RemoveAllListeners();
            buttons[0].onClick.AddListener(ResolveExtraction);
        }
    }

    private void ResolveExtraction()
    {
        int outcome = random.Next(0, 4);
        string message;
        if (outcome == 0)
        {
            cash = 0;
            message = "撤离成功，但当前现金被清空。";
        }
        else if (outcome == 1)
        {
            if (inventory.Count > 0)
            {
                string item = inventory[0];
                inventory.RemoveAt(0);
                message = "撤离成功，但丢失道具: " + item + "。";
            }
            else
            {
                cash = Mathf.Max(0, cash - 30);
                message = "撤离成功，没有道具可丢，损失 $30。";
            }
        }
        else if (outcome == 2)
        {
            debt += 500;
            message = "撤离成功，但事务所新增 $500 负债。";
        }
        else
        {
            cash = Mathf.Max(0, cash / 2);
            message = "撤离成功，但半数现金与战利品被遗失。";
        }

        CloseEvent();
        MoveTo("gate");
        ShowAgency(message);
    }

    private void ShowBackpack()
    {
        if (backpackPanel == null)
        {
            return;
        }

        sceneLocked = true;
        backpackPanel.gameObject.SetActive(true);
        backpackPanel.SetAsLastSibling();
        string text = "现金: $" + cash + "\n负债: $" + debt + "\n\n";
        text += "道具与线索:\n";
        if (inventory.Count == 0)
        {
            text += "- 暂无物品\n";
        }
        else
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                text += "- " + inventory[i] + "\n";
            }
        }

        text += "\n常备物资:\n";
        text += "- 老式左轮手枪\n";
        text += "- 急救绷带 x1\n";
        text += "- 侦探手记\n";
        backpackContentText.text = text;
    }

    private void HideBackpack()
    {
        if (backpackPanel != null)
        {
            backpackPanel.gameObject.SetActive(false);
        }
        if (eventPanel == null || !eventPanel.gameObject.activeSelf)
        {
            sceneLocked = agencyPanel != null && agencyPanel.gameObject.activeSelf;
        }
    }

    private void ShowMap()
    {
        if (mapPanel == null)
        {
            return;
        }

        sceneLocked = true;
        mapPanel.gameObject.SetActive(true);
        mapPanel.SetAsLastSibling();
        mapContentText.text = BuildMapText();
    }

    private void ShowRecruit()
    {
        if (recruitPanel == null)
        {
            return;
        }

        sceneLocked = true;
        recruitPanel.gameObject.SetActive(true);
        recruitPanel.SetAsLastSibling();
        RollRecruitCandidate();
    }

    private void ShowShop()
    {
        if (shopPanel == null)
        {
            return;
        }

        sceneLocked = true;
        shopPanel.gameObject.SetActive(true);
        shopPanel.SetAsLastSibling();
        RefreshShopPanel();
    }

    private void HideShop()
    {
        if (shopPanel != null)
        {
            shopPanel.gameObject.SetActive(false);
        }
        if (agencyPanel != null && agencyPanel.gameObject.activeSelf)
        {
            agencyPanel.SetAsLastSibling();
        }
        if ((eventPanel == null || !eventPanel.gameObject.activeSelf) &&
            (agencyPanel == null || !agencyPanel.gameObject.activeSelf))
        {
            sceneLocked = false;
        }
    }

    private void RefreshShopPanel()
    {
        if (shopContentText == null)
        {
            return;
        }

        shopContentText.text =
            "现金: $" + cash + "\n" +
            "负债: $" + debt + "\n" +
            "保险单: " + CountInventoryItem("保险单") + "\n\n" +
            "急救治疗: 全队 HP 恢复至上限。\n" +
            "心理疏导: 全队 SAN 恢复 25 点。\n" +
            "保险单: 撤离资源损失的占位道具。";
    }

    private void BuyHeal()
    {
        if (!SpendCash(30))
        {
            return;
        }

        for (int i = 0; i < party.Count; i++)
        {
            party[i].Hp = party[i].MaxHp;
        }
        Toast("全队 HP 已恢复。");
        UpdateHud();
        RefreshShopPanel();
        UpdateAgencyPanel("完成急救治疗。");
    }

    private void BuySanCare()
    {
        if (!SpendCash(40))
        {
            return;
        }

        for (int i = 0; i < party.Count; i++)
        {
            party[i].San = Mathf.Min(party[i].MaxSan, party[i].San + 25);
        }
        Toast("全队 SAN 已恢复。");
        UpdateHud();
        RefreshShopPanel();
        UpdateAgencyPanel("完成心理疏导。");
    }

    private void BuyInsurance()
    {
        if (!SpendCash(60))
        {
            return;
        }

        inventory.Add("保险单");
        Toast("获得保险单。");
        UpdateHud();
        RefreshShopPanel();
        UpdateAgencyPanel("购买保险单。");
    }

    private bool SpendCash(int amount)
    {
        if (cash < amount)
        {
            Toast("现金不足。");
            return false;
        }

        cash -= amount;
        return true;
    }

    private int CountInventoryItem(string itemName)
    {
        int count = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] == itemName)
            {
                count++;
            }
        }
        return count;
    }

    private void HideRecruit()
    {
        if (recruitPanel != null)
        {
            recruitPanel.gameObject.SetActive(false);
        }
        if (agencyPanel != null && agencyPanel.gameObject.activeSelf)
        {
            agencyPanel.SetAsLastSibling();
        }
        if ((eventPanel == null || !eventPanel.gameObject.activeSelf) &&
            (agencyPanel == null || !agencyPanel.gameObject.activeSelf))
        {
            sceneLocked = false;
        }
    }

    private void ShowTeamEdit()
    {
        if (teamEditPanel == null)
        {
            return;
        }

        sceneLocked = true;
        teamEditPanel.gameObject.SetActive(true);
        teamEditPanel.SetAsLastSibling();
        RefreshTeamEditPanel();
    }

    private void HideTeamEdit()
    {
        if (teamEditPanel != null)
        {
            teamEditPanel.gameObject.SetActive(false);
        }
        if (agencyPanel != null && agencyPanel.gameObject.activeSelf)
        {
            agencyPanel.SetAsLastSibling();
        }
        if ((eventPanel == null || !eventPanel.gameObject.activeSelf) &&
            (agencyPanel == null || !agencyPanel.gameObject.activeSelf))
        {
            sceneLocked = false;
        }
    }

    private void RefreshTeamEditPanel()
    {
        if (teamEditContentText == null)
        {
            return;
        }

        ClearRosterSlots();
        teamEditContentText.text = "正式队员\n\n\n\n\n\n候补人选 " + reserve.Count + "/" + MaxReserveCount + "\n拖动头像到另一个头像上，可交换正式队员、候补人员或调整顺序。";

        for (int i = 0; i < party.Count; i++)
        {
            float x0 = 0.08f + i * 0.29f;
            CreateRosterSlot(teamEditContentText.transform, party[i], RosterZone.Party, i, x0, 0.58f, x0 + 0.23f, 0.92f, false);
        }

        for (int i = 0; i < reserve.Count; i++)
        {
            float x0 = 0.08f + (i % 4) * 0.22f;
            float y0 = i < 4 ? 0.26f : 0.08f;
            CreateRosterSlot(teamEditContentText.transform, reserve[i], RosterZone.Reserve, i, x0, y0, x0 + 0.16f, y0 + 0.18f, true);
        }
    }

    private void ClearRosterSlots()
    {
        List<GameObject> remove = new List<GameObject>();
        for (int i = 0; i < teamEditContentText.transform.childCount; i++)
        {
            Transform child = teamEditContentText.transform.GetChild(i);
            if (child.name.StartsWith("Roster Slot"))
            {
                remove.Add(child.gameObject);
            }
        }

        for (int i = 0; i < remove.Count; i++)
        {
            remove[i].SetActive(false);
            Destroy(remove[i]);
        }
    }

    private void CreateRosterSlot(Transform parent, Investigator investigator, RosterZone zone, int index, float minX, float minY, float maxX, float maxY, bool small)
    {
        RectTransform slot = CreatePanel("Roster Slot " + zone + " " + index, parent, new Color(0.18f, 0.13f, 0.09f, 0.28f));
        Anchor(slot, minX, minY, maxX, maxY, 0f, 0f, 0f, 0f);

        Image portraitBack = CreateImage("Roster Portrait Back", slot, new Color(0.95f, 0.89f, 0.72f, 1f));
        Anchor(portraitBack.rectTransform, 0.18f, small ? 0.28f : 0.36f, 0.82f, 0.98f, 0f, 0f, 0f, 0f);
        Image portrait = CreateImage("Roster Portrait", portraitBack.transform, Color.white);
        portrait.sprite = GetSprite(investigator.PortraitKey);
        portrait.preserveAspect = true;
        Stretch(portrait.rectTransform);

        Text name = CreateText("Roster Name", slot, investigator.Job + "\n" + investigator.Name, small ? 10 : 12, TextAnchor.MiddleCenter, new Color(0.08f, 0.055f, 0.035f, 1f));
        Anchor(name.rectTransform, 0f, 0f, 1f, small ? 0.28f : 0.34f, 0f, 0f, 0f, 0f);

        RosterDragItem drag = slot.gameObject.AddComponent<RosterDragItem>();
        drag.Owner = this;
        drag.Zone = zone;
        drag.Index = index;
    }

    private void BeginRosterDrag(RosterDragItem item)
    {
        activeRosterDrag = item;
        if (rosterDragGhost == null)
        {
            rosterDragGhost = CreateImage("Roster Drag Ghost", root, Color.white);
            rosterDragGhost.preserveAspect = true;
            rosterDragGhost.raycastTarget = false;
            rosterDragGhost.rectTransform.sizeDelta = new Vector2(72f, 96f);
        }

        Investigator investigator = GetRosterInvestigator(item.Zone, item.Index);
        rosterDragGhost.sprite = investigator == null ? null : GetSprite(investigator.PortraitKey);
        rosterDragGhost.gameObject.SetActive(true);
        rosterDragGhost.transform.SetAsLastSibling();
    }

    private void UpdateRosterDrag(PointerEventData eventData)
    {
        if (rosterDragGhost == null)
        {
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, eventData.pressEventCamera, out localPoint);
        rosterDragGhost.rectTransform.anchoredPosition = localPoint;
    }

    private void EndRosterDrag()
    {
        if (rosterDragGhost != null)
        {
            rosterDragGhost.gameObject.SetActive(false);
        }
        activeRosterDrag = null;
    }

    private void DropRosterOn(RosterDragItem target)
    {
        if (activeRosterDrag == null || target == null)
        {
            return;
        }

        SwapRoster(activeRosterDrag.Zone, activeRosterDrag.Index, target.Zone, target.Index);
        EndRosterDrag();
    }

    private Investigator GetRosterInvestigator(RosterZone zone, int index)
    {
        List<Investigator> list = zone == RosterZone.Party ? party : reserve;
        if (index < 0 || index >= list.Count)
        {
            return null;
        }
        return list[index];
    }

    private void ShowRosterDetail(RosterZone zone, int index)
    {
        Investigator investigator = GetRosterInvestigator(zone, index);
        if (investigator == null || rosterDetailPanel == null)
        {
            return;
        }

        rosterDetailPortrait.sprite = GetSprite(investigator.PortraitKey);
        rosterDetailText.text =
            investigator.Name + " / " + investigator.Job + "\n\n" +
            "HP    " + investigator.Hp + "/" + investigator.MaxHp + "\n" +
            "SAN   " + investigator.San + "%\n\n" +
            "力量 STR    " + investigator.Str + "\n" +
            "敏捷 DEX    " + investigator.Dex + "\n" +
            "体质 CON    " + investigator.Con + "\n" +
            "智力 INT    " + investigator.Int + "\n" +
            "魅力 CHA    " + investigator.Cha + "\n" +
            "幸运 LUCK   " + investigator.Luck + "\n\n" +
            (zone == RosterZone.Party ? "状态: 正式队员" : "状态: 候补人员");

        rosterDetailPanel.gameObject.SetActive(true);
        rosterDetailPanel.SetAsLastSibling();
    }

    private void HideRosterDetail()
    {
        if (rosterDetailPanel != null)
        {
            rosterDetailPanel.gameObject.SetActive(false);
        }
        if (teamEditPanel != null && teamEditPanel.gameObject.activeSelf)
        {
            teamEditPanel.SetAsLastSibling();
        }
    }

    private void SwapRoster(RosterZone fromZone, int fromIndex, RosterZone toZone, int toIndex)
    {
        List<Investigator> fromList = fromZone == RosterZone.Party ? party : reserve;
        List<Investigator> toList = toZone == RosterZone.Party ? party : reserve;
        if (fromIndex < 0 || fromIndex >= fromList.Count || toIndex < 0 || toIndex >= toList.Count)
        {
            return;
        }

        Investigator temp = fromList[fromIndex];
        fromList[fromIndex] = toList[toIndex];
        toList[toIndex] = temp;
        RefreshPortraitSlots();
        UpdateHud();
        RefreshTeamEditPanel();
        UpdateAgencyPanel("队伍编制已更新。");
    }

    private void RollRecruitCandidate()
    {
        string[] names = { "格兰特", "梅布尔", "周医生", "诺亚", "温斯顿", "林记者", "维克多" };
        string[] jobs = { "侦探", "学者", "小偷", "探险家", "打手", "警察", "记者" };
        recruitCandidate = new Investigator();
        recruitCandidate.Name = Pick(names);
        recruitCandidate.Job = Pick(jobs);
        recruitCandidate.Str = Roll3D6();
        recruitCandidate.Dex = Roll3D6();
        recruitCandidate.Con = Roll3D6();
        recruitCandidate.Int = Roll3D6();
        recruitCandidate.Cha = Roll3D6();
        recruitCandidate.Luck = Roll3D6();
        recruitCandidate.MaxHp = Mathf.Max(8, recruitCandidate.Con + random.Next(-2, 3));
        recruitCandidate.Hp = recruitCandidate.MaxHp;
        recruitCandidate.MaxSan = 100;
        recruitCandidate.San = Mathf.Clamp(recruitCandidate.Int * 5 + random.Next(-10, 11), 35, 95);
        recruitPortraitKey = portraitKeys.Count == 0 ? "portrait_detective" : portraitKeys[random.Next(0, portraitKeys.Count)];
        recruitCandidate.PortraitKey = recruitPortraitKey;

        recruitPortraitImage.sprite = GetSprite(recruitPortraitKey);
        recruitNameText.text = recruitCandidate.Name + " / " + recruitCandidate.Job;
        recruitStatsText.text =
            "力量 STR  " + recruitCandidate.Str + "\n" +
            "敏捷 DEX  " + recruitCandidate.Dex + "\n" +
            "体质 CON  " + recruitCandidate.Con + "\n" +
            "智力 INT  " + recruitCandidate.Int + "\n" +
            "魅力 CHA  " + recruitCandidate.Cha + "\n" +
            "幸运 LUCK " + recruitCandidate.Luck + "\n\n" +
            "HP " + recruitCandidate.Hp + "/" + recruitCandidate.MaxHp + "   SAN " + recruitCandidate.San + "%";
    }

    private void HireRecruitCandidate()
    {
        if (recruitCandidate == null)
        {
            RollRecruitCandidate();
            return;
        }

        if (reserve.Count >= MaxReserveCount)
        {
            Toast("候补名单已满，最多可存储 " + MaxReserveCount + " 人。");
            return;
        }

        reserve.Add(recruitCandidate);

        RefreshPortraitSlots();

        Toast("已录用 " + recruitCandidate.Name + "，加入候补 " + reserve.Count + "/" + MaxReserveCount + "。");
        recruitCandidate = null;
        UpdateHud();
        UpdateAgencyPanel("新调查员已加入候补名单。");
        HideRecruit();
    }

    private int Roll3D6()
    {
        return random.Next(1, 7) + random.Next(1, 7) + random.Next(1, 7);
    }

    private void HideMap()
    {
        if (mapPanel != null)
        {
            mapPanel.gameObject.SetActive(false);
        }
        if (eventPanel == null || !eventPanel.gameObject.activeSelf)
        {
            sceneLocked = agencyPanel != null && agencyPanel.gameObject.activeSelf;
        }
    }

    private void ShowAgency(string message)
    {
        if (agencyPanel == null)
        {
            return;
        }

        agencyPanel.gameObject.SetActive(true);
        agencyPanel.SetAsLastSibling();
        sceneLocked = true;
        UpdateAgencyPanel(message);
    }

    private void StartInvestigationFromAgency()
    {
        if (agencyPanel != null)
        {
            agencyPanel.gameObject.SetActive(false);
        }
        if (backpackPanel != null)
        {
            backpackPanel.gameObject.SetActive(false);
        }
        if (mapPanel != null)
        {
            mapPanel.gameObject.SetActive(false);
        }

        sceneLocked = false;
        GenerateInvestigation();
        MoveTo("gate");
        Toast("特情处签发新调查令。");
    }

    private void UpdateAgencyPanel(string message)
    {
        if (agencyStatusText == null || agencyTeamText == null)
        {
            return;
        }

        agencyStatusText.text =
            "现金: $" + cash + "\n" +
            "负债: $" + debt + "\n" +
            "背包物品: " + inventory.Count + "\n" +
            "候补人数: " + reserve.Count + "/" + MaxReserveCount + "\n" +
            "当前专案: " + activeRunName + "\n" +
            "最近报告: " + message;

        string team = "待命调查员:\n";
        for (int i = 0; i < party.Count; i++)
        {
            Investigator p = party[i];
            team += p.Name + " / " + p.Job + "   HP " + p.Hp + "   SAN " + p.San + "\n";
        }
        team += "\n下一步: 重新进入锯木厂，继续处理未完成事件。";
        agencyTeamText.text = team;
    }

    private string BuildMapText()
    {
        string gate = MapLine("gate");
        string hall = MapLine("hall");
        string generator = MapLine("generator");
        string coldroom = MapLine("coldroom");
        string office = MapLine("office");
        return
            activeRunName + "\n\n" +
            "        " + office + "\n" +
            "          │\n" +
            generator + " ─ " + hall + " ─ " + coldroom + "\n" +
            "          │\n" +
            "        " + gate + "\n\n" +
            "标记说明:\n" +
            ">> 当前位置\n" +
            "√ 已处理事件\n" +
            "! 存在未处理事件\n";
    }

    private string MapLine(string nodeId)
    {
        SceneNode node = nodes[nodeId];
        string marker = nodeId == currentNode.Id ? ">>" : "  ";
        string state = node.EventType == DemoEventType.None ? " " : (node.Completed ? "√" : "!");
        return marker + state + node.Title;
    }

    private void CloseEvent()
    {
        Button[] buttons = eventPanel.GetComponentsInChildren<Button>();
        if (buttons.Length > 0)
        {
            buttons[0].onClick.RemoveAllListeners();
            buttons[0].onClick.AddListener(ResolveEvent);
        }
        eventPanel.gameObject.SetActive(false);
        sceneLocked = false;
        inBattle = false;
        UpdateHud();
    }

    private void Toast(string message)
    {
        resultPanel.gameObject.SetActive(true);
        resultText.text = message;
        CancelInvoke("HideToast");
        Invoke("HideToast", 2.2f);
    }

    private void HideToast()
    {
        resultPanel.gameObject.SetActive(false);
    }

    private void UpdateHud()
    {
        RefreshPortraitSlots();
        cashText.text = "$" + cash;
        debtText.text = "Debt $" + debt;
        string partyLine = activeRunName == "" ? "CASE: MISSING HEIR" : "CASE: " + activeRunName;
        for (int i = 0; i < party.Count; i++)
        {
            Investigator p = party[i];
            if (investigatorNameTexts != null && i < investigatorNameTexts.Length && investigatorNameTexts[i] != null)
            {
                investigatorNameTexts[i].text = p.Job.ToUpper();
            }
            if (hpFillRects != null && i < hpFillRects.Length && hpFillRects[i] != null)
            {
                SetBarFill(hpFillRects[i], p.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)p.Hp / (float)p.MaxHp));
            }
            if (sanFillRects != null && i < sanFillRects.Length && sanFillRects[i] != null)
            {
                SetBarFill(sanFillRects[i], p.MaxSan <= 0 ? 0f : Mathf.Clamp01((float)p.San / (float)p.MaxSan));
            }
            if (hpValueTexts != null && i < hpValueTexts.Length && hpValueTexts[i] != null)
            {
                hpValueTexts[i].text = p.Hp + "/" + p.MaxHp;
            }
            if (sanValueTexts != null && i < sanValueTexts.Length && sanValueTexts[i] != null)
            {
                sanValueTexts[i].text = p.San + "%";
            }
        }
        partyText.text = partyLine;
    }

    private Sprite GetSprite(string key)
    {
        if (sprites.ContainsKey(key))
        {
            return sprites[key];
        }
        return null;
    }

    private void SetBarFill(RectTransform fillRect, float percent)
    {
        percent = Mathf.Clamp01(percent);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(percent, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private void RefreshPortraitSlots()
    {
        if (portraitImages == null)
        {
            return;
        }

        for (int i = 0; i < portraitImages.Length; i++)
        {
            if (portraitImages[i] == null)
            {
                continue;
            }

            if (i < party.Count && !String.IsNullOrEmpty(party[i].PortraitKey))
            {
                portraitImages[i].sprite = GetSprite(party[i].PortraitKey);
            }
        }
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        Image image = CreateImage(name, parent, color);
        return image.rectTransform;
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, int size, Color color)
    {
        Image image = CreateImage(name, parent, color);
        Button button = image.gameObject.AddComponent<Button>();
        Text text = CreateText("Label", image.transform, label, size, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        return button;
    }

    private void Stretch(RectTransform rect)
    {
        Anchor(rect, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f);
    }

    private void Anchor(RectTransform rect, float minX, float minY, float maxX, float maxY, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }
}
