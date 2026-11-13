using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinematic;
using OpeningEnding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CinematicEditor
{
    public static partial class CinematicSceneBuilder
    {
        private const string DataPath = "Assets/Data/OpeningEnding/CinematicText.asset";
        private const string ResultPath = "Assets/Data/OpeningEnding/CinematicModelResult.asset";
        private static Font _font;
        private static Material _wood, _paper, _metal, _dark, _cyan, _amber, _green;
        private static CinematicText _content;

        [MenuItem("Tools/Factory/Create Documentary Opening and Ending")]
        public static void Create()
        {
            _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pretendard/Pretendard-SemiBold.otf");
            foreach (string path in new[] { "Assets/Data/OpeningEnding", "Assets/Materials/OpeningEnding" })
            { Directory.CreateDirectory(path); }
            AssetDatabase.Refresh();
            PreparePostalModels();
            ApplyPostalTextures();
            _wood = Material("ArchiveOak", new Color(.23f,.125f,.064f));
            _paper = Material("ArchivePaper", new Color(.76f,.69f,.52f));
            _metal = Material("ArchiveMetal", new Color(.2f,.25f,.22f));
            _dark = Material("ArchiveCharcoal", new Color(.025f,.032f,.033f));
            _cyan = Material("ArchiveCyan", new Color(.18f,.77f,.72f), true);
            _amber = Material("ArchiveAmber", new Color(1,.52f,.13f), true);
            _green = Material("ArchiveCRT", new Color(.26f,.9f,.46f), true);
            _content = AssetDatabase.LoadAssetAtPath<CinematicText>(DataPath);
            if (_content == null) { _content = ScriptableObject.CreateInstance<CinematicText>(); AssetDatabase.CreateAsset(_content, DataPath); FillContent(); }
            CinematicModelResult result = AssetDatabase.LoadAssetAtPath<CinematicModelResult>(ResultPath);
            if (result == null) { result = ScriptableObject.CreateInstance<CinematicModelResult>(); AssetDatabase.CreateAsset(result, ResultPath); }
            Build(false, result); Build(true, result);
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == "Assets/Scenes/CNNArchiveOpening.unity" || s.path == "Assets/Scenes/CNNArchiveEnding.unity");
            scenes.Insert(0, new EditorBuildSettingsScene("Assets/Scenes/CNNArchiveOpening.unity", true));
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/CNNArchiveEnding.unity", true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Cinematic] Two documentary scenes saved with XR rig, media, sound and skip input.");
        }

        private static CinematicText.Line Line(double start, double end, string text,
            CinematicText.CaptionAnchor anchor = CinematicText.CaptionAnchor.Start)
        {
            return new CinematicText.Line { Anchor = anchor, Start = start, End = end, Text = text };
        }
        private static void FillContent()
        {
            _content.Opening = new[] {
                Line(0.5,3.1,"기계는 세상을 어떻게 볼까요?"),
                Line(1.1,5.55,"최초의 CNN이 읽은 것은\n봉투에 손으로 쓴 우편번호입니다.", CinematicText.CaptionAnchor.PostOffice),
                Line(.25,3.98,"사람의 눈은 봉투에 적힌 우편번호를\n한눈에 읽습니다.", CinematicText.CaptionAnchor.Envelope),
                Line(.05,3.9,"그러나 기계가 보는 건\n밝고 어두운 점들입니다.", CinematicText.CaptionAnchor.Pixels),
                Line(4.25,8.55,"기계는 이 점들 속에서 어떻게\n숫자를 알아낼 수 있을까요?", CinematicText.CaptionAnchor.Pixels),
                Line(.05,3.65,"{C:공장 안에서} 그 답을 직접 찾아보세요.", CinematicText.CaptionAnchor.Doors) };
            for (int i = 0; i < _content.Opening.Length; i++)
                _content.Opening[i].Voice = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/OpeningEnding/OpeningNarration{i + 1:D2}.wav");
            _content.OpeningCrowd = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OpeningEnding/OpeningCrowd.mp3");
            _content.OpeningScore = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OpeningEnding/OpeningScore.wav");
            _content.Ending = new[] {
                Line(.4,2.8,"1989년,\n숫자 하나를 읽던 눈은"),
                Line(0,1.6,"그리고 오늘,", CinematicText.CaptionAnchor.Recognition),
                Line(.2,1.4,"{A:당신이 만든 눈.}", CinematicText.CaptionAnchor.Model),
                Line(.2,2,"그 시선의 끝에,\n당신의 CNN이 있다.", CinematicText.CaptionAnchor.Closing) };
            Texture2D post = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/OpeningEnding/PostOffice_US_1989.png");
            Texture2D montage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/OpeningEnding/EverydayVisionAtlas.png");
            _content.PostOffice = new CinematicText.ArchiveClip { Id="PO_US_1989", Still=post, Source="OpenAI image_gen · 1989년 미국 우체국 분위기 재구성 · 실제 기록사진 아님" };
            string[] years = { "1998", "2012", "2015", "2018", "2020", "2022", "2024", "2026" };
            string[] labels = { "725", "사진 분류", "얼굴", "12가 3456", "양말 · 전선", "표지판 30", "확인 필요", "고양이" };
            double[] durations = { 1.8, 1.6, 1.4, 1.3, 1.2, 1.2, 1.2, 1.3 };
            _content.EndingSchedule.Montage = new EndingSequenceData.MontageEntry[years.Length];
            for (int i = 0; i < years.Length; i++)
            {
                _content.EndingSchedule.Montage[i] = new EndingSequenceData.MontageEntry
                {
                    Year = years[i],
                    Label = labels[i],
                    PropIndex = i,
                    Duration = durations[i],
                    Clip = new CinematicText.ArchiveClip
                    {
                        Id = "END_" + years[i],
                        Still = montage,
                        UV = new Rect((i % 4) * .25f, i < 4 ? .5f : 0, .25f, .5f),
                        Duration = 2,
                        Source = "OpenAI image_gen · 일상 속 시각 인식 재구성 · 연도는 연출용, 기술 최초 출시 연도가 아님"
                    }
                };
            }
            _content.Type=Audio("Type"); _content.Hit=Audio("Hit"); _content.BigHit=Audio("BigHit");
            _content.Rewind=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OpeningEnding/OpeningTimelinePulse.wav"); _content.Stamp=Audio("Stamp"); _content.Beep=Audio("Beep");
            EditorUtility.SetDirty(_content);
        }
        private static AudioClip Audio(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OpeningEnding/Archive"+name+".wav");

        private static void Build(bool ending, CinematicModelResult result)
        {
            string name=ending ? "CNNArchiveEnding" : "CNNArchiveOpening";
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            result=AssetDatabase.LoadAssetAtPath<CinematicModelResult>(ResultPath);
            GameObject rig=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/XR/XRPlayerRig.prefab"));
            Camera camera=rig.GetComponentInChildren<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            camera.farClipPlane=45;
            RenderSettings.skybox=null; RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.54f,.50f,.42f); RenderSettings.fog=false;
            Light light=new GameObject("Soft window light",typeof(Light)).GetComponent<Light>();
            light.type=LightType.Directional; light.intensity=.9f; light.color=new Color(1,.87f,.67f);
            light.transform.rotation=Quaternion.Euler(45,-40,0); light.shadows=LightShadows.None;
            Transform stage=Root("Documentary stage",null);
            float stageLift = ending ? 0f : .85f;
            stage.localPosition = Vector3.up * stageLift;
            LineRenderer platform=Root("Quiet standing marker",stage).gameObject.AddComponent<LineRenderer>();
            platform.transform.localPosition = Vector3.down * stageLift;
            platform.useWorldSpace=false;platform.loop=true;platform.positionCount=64;platform.startWidth=platform.endWidth=.006f;platform.sharedMaterial=_metal;
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;platform.SetPosition(i,new Vector3(Mathf.Sin(a)*.65f,.015f,Mathf.Cos(a)*.65f));}
            Transform system=Root("Sequence",null);
            CinematicSequenceManager sequence=system.gameObject.AddComponent<CinematicSequenceManager>();
            CinematicPresentationManager presentation=system.gameObject.AddComponent<CinematicPresentationManager>();
            CinematicInputController input=system.gameObject.AddComponent<CinematicInputController>();
            CinematicMovementController gate=system.gameObject.AddComponent<CinematicMovementController>();
            CinematicInteractionManager interaction = system.gameObject.AddComponent<CinematicInteractionManager>();
            CinematicControlsController controls = system.gameObject.AddComponent<CinematicControlsController>();
            MonoBehaviour world = ending
                ? (MonoBehaviour)system.gameObject.AddComponent<EndingVisualController>()
                : system.gameObject.AddComponent<OpeningVisualController>();
            CinematicAudioController audio=system.gameObject.AddComponent<CinematicAudioController>();
            CinematicTypeController type=system.gameObject.AddComponent<CinematicTypeController>();
            Assign(interaction, "_input", input);
            Assign(interaction, "_movement", gate);
            Assign(interaction, "_controls", controls);
            Assign(sequence,"_presentation",presentation); Assign(sequence,"_interaction",interaction); Assign(sequence,"_content",_content); Assign(sequence,"_model",result);
            Value(sequence,"_ending",ending); Value(sequence,"_destination",ending?"CNNArchiveOpening":"FactoryEnvironment");
            Assign(presentation, ending ? "_endingView" : "_openingView", world); Assign(presentation,"_audio",audio); Assign(presentation,"_type",type);
            Assign(world,"_world",stage);
            if (!ending)
            {
                Assign(world, "_postOffice", BuildPostOffice(stage, world));
                BuildPostalProps(stage, world);
            }
            Transform portal=Primitive("Archive surface",stage,new Vector3(0,1.85f,3),new Vector3(1.6f,1.067f,1),_dark,PrimitiveType.Quad);
            Material film=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/OpeningEnding/ArchiveFilm.mat");
            if (film==null) { film=new Material(Shader.Find("CNN/ArchivePhoto")); AssetDatabase.CreateAsset(film,"Assets/Materials/OpeningEnding/ArchiveFilm.mat"); }
            portal.GetComponent<Renderer>().sharedMaterial=film;
            VideoPlayer video=system.gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake=false; video.renderMode=VideoRenderMode.RenderTexture; video.audioOutputMode=VideoAudioOutputMode.None; video.waitForFirstFrame=true;
            CinematicArchiveController archive=system.gameObject.AddComponent<CinematicArchiveController>();
            Assign(archive,"_surface",portal.GetComponent<Renderer>()); Assign(archive,"_player",video);
            Assign(presentation,"_archive",archive); Assign(world,"_portal",portal);
            Canvas yearCanvas=CanvasAt(stage,"Year",new Vector3(0,1.9f,3),new Vector2(1000,260),.003f);
            Text year=TextAt(yearCanvas.transform,"Year","1989",Vector2.zero,new Vector2(1000,220),ending ? 100 : 110);
            Assign(world,"_year",year);
            Vector2 archiveCaptionSize = ending ? new Vector2(1500,100) : new Vector2(1680,190);
            Canvas captionCanvas=CanvasAt(stage,"Archive caption",ending ? new Vector3(0,.45f,3) : new Vector3(0,1.1f,1.2f),archiveCaptionSize,ending ? .002f : .0008888889f);
            if (!ending)
            {
                ImageAt(captionCanvas.transform,"Archive caption backing",archiveCaptionSize,new Color(0,0,0,.75f));
                captionCanvas.gameObject.SetActive(false);
            }
            Text caption=TextAt(captionCanvas.transform,"Provenance",_content.Provenance,Vector2.zero,ending ? archiveCaptionSize : new Vector2(1600,175),ending ? 21 : 42);
            caption.color=ending ? new Color(.75f,.72f,.63f) : new Color(.98f,.95f,.87f); Assign(world,"_caption",caption);
            Transform timeline=Root("Rewinding history",stage);
            for(int i=0;i<15;i++)
            {
                float x=-i*.85f;
                Box("Timeline rule",timeline,new Vector3(x,.02f,0),new Vector3(.81f,.009f,.015f),_amber);
                Canvas c=CanvasAt(timeline,"Year marker",new Vector3(x,.15f,0),new Vector2(250,90),.002f);
                TextAt(c.transform,"Year",(2026-i*3).ToString(),Vector2.zero,new Vector2(250,90),ending ? 32 : 62);
            }
            Assign(world,"_timeline",timeline);
            List<Transform> boxes=new List<Transform>();
            for(int i=0;i<10;i++)
            {
                Vector3 p=new Vector3((i%5-2)*.72f,1.3f+(i/5)*.65f,3.8f+(i%2)*.35f);
                Transform box=Recognition(stage,"Recognition "+i,p+new Vector3(0,0,-.03f),new Vector2(.45f,.27f),_green,i==0?"12345":(12000+i*173).ToString()); boxes.Add(box);
            }
            Array(world,"_boxes",boxes);
            Vector3 digitPosition = new Vector3(0, ending ? 1.9f : 1.25f, 2.4f);
            Transform digit=Root("Handwritten digit",stage); digit.localPosition=digitPosition;
            List<Transform> pixels=new List<Transform>();
            for(int row=0;row<16;row++) for(int col=0;col<16;col++)
            {
                float x=(col-7.5f)/7.5f, y=(row-7.5f)/7.5f;
                bool ink=(row>=2&&row<=4&&col>=4&&col<=11)||(col>=10&&col<=12&&row>=3&&row<=7)
                    ||(row>=6&&row<=11&&Mathf.Abs(col-(17-row))<2)||(row>=11&&row<=13&&col>=3&&col<=12);
                if(!ink) {continue;}
                float spread = ending ? .8f : .48f;
                Vector3 local=new Vector3(x*spread,-y*spread,0);
                pixels.Add(Box("Light pixel",stage,local+digitPosition,Vector3.one*.085f,_cyan));
            }
            Canvas handwritten=CanvasAt(digit,"Handwritten numeral",Vector3.zero,new Vector2(330,330),ending ? .008f : .0048f);
            Text numeral=TextAt(handwritten.transform,"Numeral","2",Vector2.zero,new Vector2(330,330),280);
            numeral.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumPenScript-Regular.ttf");
            Assign(world,"_digit",digit);
            Array(world,"_pixels",pixels);
            if (!ending) { Assign(world,"_numeral",numeral); }
            Transform digitBox = Recognition(digit,"Digit recognition",new Vector3(0,0,-.06f),new Vector2(1.7f,1.7f),_green,"2",ending ? 25 : 56);
            if (!ending) { digitBox.localScale = Vector3.one * .6f; }
            if (!ending) { Assign(world,"_digitBox",digitBox); }
            Transform factory=Root("CNN facade",stage); factory.localPosition=new Vector3(0,0,12);
            if (!ending) { factory.localPosition = new Vector3(0, 0, 8.5f); }
            if (ending)
            {
                Box("Facade",factory,new Vector3(0,2.5f,.7f),new Vector3(12,5,.7f),_dark);
                Box("Door light",factory,new Vector3(0,1.5f,.15f),new Vector3(3.1f,3,.1f),_cyan);
                Box("Left door",factory,new Vector3(-.7f,1.5f,0),new Vector3(1.4f,3,.18f),_metal);
                Box("Right door",factory,new Vector3(.7f,1.5f,0),new Vector3(1.4f,3,.18f),_metal);
                for(int i=0;i<18;i++)
                { float x=(i%9-4)*1.05f; float y=3.5f+(i/9)*.6f; Box("Circuit",factory,new Vector3(x,y,-.02f),new Vector3(.7f,.018f,.02f),_cyan); }
                Canvas titleCanvas=CanvasAt(factory,"Factory title",new Vector3(0,4.55f,-.1f),new Vector2(1200,220),.004f);
                TextAt(titleCanvas.transform,"Title",_content.FactoryTitle,Vector2.zero,new Vector2(1200,220),90);
                for(int i=0;i<22;i++) {Box("Path light",factory,new Vector3(0,.012f,-i*.5f),new Vector3(.8f,.015f,.045f),_cyan);}
            }
            else
            {
                BuildOpeningEntrance(factory, world);
            }
            Assign(world,"_factory",factory);
            List<Transform> eraProps=new List<Transform>();
            for(int i=0;i<8;i++)
            {
                Transform prop=Root("Era prop "+_content.EndingSchedule.Montage.Single(entry => entry.PropIndex == i).Year,stage); prop.localPosition=new Vector3(0,0,1.8f);
                BuildEraProp(prop,i); eraProps.Add(prop);
            }
            Array(world,"_eraProps",eraProps);
            Transform network=Root("Your CNN miniature",stage); network.localPosition=new Vector3(0,1.75f,2.4f);
            Recognition(network,"Amber frame",Vector3.zero,new Vector2(2.4f,1.0f),_amber,"");
            List<Transform> layers=new List<Transform>(); List<Text> labels=new List<Text>();
            for(int i=0;i<10;i++)
            {
                Transform layer=Root("Layer "+i,network); layer.localPosition=new Vector3((i-4.5f)*.14f,0,0); layers.Add(layer);
                for(int j=0;j<4;j++) {Box("Feature slice",layer,new Vector3(j*.012f,0,j*.025f),new Vector3(.07f,.28f-(i%3)*.035f,.008f),i==9?_amber:_cyan);}
                Canvas c=CanvasAt(layer,"Label",new Vector3(0,-.28f,-.02f),new Vector2(160,180),.0013f);
                labels.Add(TextAt(c.transform,"Layer name","",Vector2.zero,new Vector2(160,180),26));
            }
            Canvas note=CanvasAt(network,"Result status",new Vector3(0,.58f,0),new Vector2(1200,100),.0016f);
            Text networkNote = TextAt(note.transform,"Note","",Vector2.zero,new Vector2(1200,100),26);
            Assign(world,"_network",network);
            if (ending)
            {
                Assign(world,"_networkNote",networkNote);
                Array(world,"_networkLayers",layers);
                Array(world,"_layerLabels",labels);
            }
            Canvas captions=CanvasAt(stage,"Narration",new Vector3(0,ending ? .99f : .5f,2.5f),ending ? new Vector2(1500,260) : new Vector2(1740,200),.0018f);
            if (!ending)
            {
                captions.overrideSorting = true;
                captions.sortingOrder = 10;
            }
            Image backing=ImageAt(captions.transform,"Caption backing",ending ? new Vector2(1500,245) : new Vector2(1740,200),new Color(0,0,0,ending ? .45f : .88f));
            Text text=TextAt(captions.transform,"Type line","",Vector2.zero,ending ? new Vector2(1450,240) : new Vector2(1660,190),ending ? 48 : 62);
            Value(type, "_drawOverScene", !ending);
            Assign(type,"_line",text); Assign(type,"_backing",backing);
            Canvas hints=CanvasAt(stage,"Trigger hold",new Vector3(0,ending ? .4f : .64f,2.3f),new Vector2(1100,120),.0015f);
            Image bar=ImageAt(hints.transform,"Hold progress",new Vector2(500,5),new Color(.3f,.8f,.73f));
            bar.rectTransform.anchoredPosition=new Vector2(0,-35); bar.type=Image.Type.Filled; bar.fillMethod=Image.FillMethod.Horizontal; bar.fillAmount=0;
            bar.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Assign(controls,"_holdBar",bar);
            Assign(controls,"_failureText",text);
            Assign(controls,"_failureBacking",backing);
            if (ending)
            {
                Canvas completion = CanvasAt(stage, "Ending Completion Panel", new Vector3(0, 1.6f, 2.15f), new Vector2(720, 370), .0018f);
                TextAt(completion.transform, "Completion Title", "CNN 공장 · 체험 완료", new Vector2(0, 135), new Vector2(720, 110), 54);
                Button restart = CreateCinematicButton(completion, "Restart Button", "처음부터 다시 시작", 660);
                Assign(controls, "_restartButton", restart);
                completion.gameObject.SetActive(false);
            }
            else
            {
                Canvas start = CanvasAt(stage, "Opening Start Panel", new Vector3(0, .75f, 2.15f), new Vector2(420, 122), .0018f);
                Button startButton = CreateCinematicButton(start, "Start Button", "시작", 420, 76);
                Assign(controls, "_startButton", startButton);
            }
            BuildAudio(stage,audio);
            ConfigureNarrationAudio(audio, !ending);
            factory.gameObject.SetActive(false); network.gameObject.SetActive(false);
            foreach(Transform t in boxes.Concat(pixels).Concat(eraProps)) {t.gameObject.SetActive(false);}
            digit.gameObject.SetActive(false); portal.gameObject.SetActive(false); timeline.gameObject.SetActive(false);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/"+name+".unity");
        }

        private static void BuildPostalProps(Transform stage, MonoBehaviour world)
        {
            Material postalBlue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeBlue.mat");
            Material postalBrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeBrass.mat");
            Material postalFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeFloor.mat");
            Material postalInk = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeInk.mat");
            Material postalLight = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeLight.mat");
            Material postalOak = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeOak.mat");
            Material postalPaper = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficePaper.mat");
            Material postalPlaster = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficePlaster.mat");
            Transform postal0 = Root("Postal envelope", stage);
            postal0.localPosition = new Vector3(0.25f, 0.73f, 1.85f);
            postal0.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            postal0.localRotation = new Quaternion(0.7071068f, 0f, 0f, 0.7071068f);
            postal0.gameObject.SetActive(false);
            Transform postal1 = Box("Envelope paper", postal0, new Vector3(0f, 0f, 0f), new Vector3(1f, 0.56f, 0.022f), postalPaper);
            Transform postal2 = Box("Sender address", postal0, new Vector3(-0.3f, 0.16f, -0.013f), new Vector3(0.19f, 0.008f, 0.003f), postalBrass);
            Transform postal3 = Box("Sender address", postal0, new Vector3(-0.3f, 0.115f, -0.013f), new Vector3(0.25f, 0.008f, 0.003f), postalBrass);
            Transform postal4 = Box("Sender address", postal0, new Vector3(-0.3f, 0.07f, -0.013f), new Vector3(0.16f, 0.008f, 0.003f), postalBrass);
            Transform postal5 = Box("Postage paper", postal0, new Vector3(0.36f, 0.16f, -0.014f), new Vector3(0.16f, 0.17f, 0.006f), postalBlue);
            Transform postal6 = Box("Postage stripe", postal0, new Vector3(0.36f, 0.12f, -0.019f), new Vector3(0.11f, 0.008f, 0.003f), postalPaper);
            Transform postal7 = Box("Postage stripe", postal0, new Vector3(0.36f, 0.16f, -0.019f), new Vector3(0.11f, 0.008f, 0.003f), postalPaper);
            Transform postal8 = Box("Postage stripe", postal0, new Vector3(0.36f, 0.2f, -0.019f), new Vector3(0.11f, 0.008f, 0.003f), postalPaper);
            Transform postal9 = Box("Envelope flap", postal0, new Vector3(-0.24f, 0.11f, 0.013f), new Vector3(0.5660389f, 0.009f, 0.003f), postalBrass);
            postal9.localRotation = new Quaternion(0f, 0f, -0.2756825f, 0.9612487f);
            Transform postal10 = Box("Envelope flap", postal0, new Vector3(0.24f, 0.11f, 0.013f), new Vector3(0.5660389f, 0.009f, 0.003f), postalBrass);
            postal10.localRotation = new Quaternion(0f, 0f, 0.2756825f, 0.9612487f);
            Transform postal11 = Root("ZIP digit 2", postal0);
            postal11.localPosition = new Vector3(0.04f, -0.12f, -0.018f);
            postal11.localScale = new Vector3(0.7f, 0.7f, 1f);
            Transform postal12 = Box("Ink stroke", postal11, new Vector3(-0.0375f, 0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal12.localRotation = new Quaternion(0f, 0f, 0.4241554f, 0.9055894f);
            Transform postal13 = Box("Ink stroke", postal11, new Vector3(0.0f, 0.08f, 0f), new Vector3(0.05f, 0.012f, 0.003f), postalInk);
            Transform postal14 = Box("Ink stroke", postal11, new Vector3(0.0375f, 0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal14.localRotation = new Quaternion(0f, 0f, -0.4241554f, 0.9055894f);
            Transform postal15 = Box("Ink stroke", postal11, new Vector3(0.05f, 0.0325f, 0f), new Vector3(0.035f, 0.012f, 0.003f), postalInk);
            postal15.localRotation = new Quaternion(0f, 0f, -0.7071068f, 0.7071068f);
            Transform postal16 = Box("Ink stroke", postal11, new Vector3(0.0f, -0.0325f, 0f), new Vector3(0.1379311f, 0.012f, 0.003f), postalInk);
            postal16.localRotation = new Quaternion(0f, 0f, -0.9287086f, 0.3708103f);
            Transform postal17 = Box("Ink stroke", postal11, new Vector3(0.0025f, -0.08f, 0f), new Vector3(0.105f, 0.012f, 0.003f), postalInk);
            Transform postal18 = Root("ZIP digit 0", postal0);
            postal18.localPosition = new Vector3(0.13f, -0.12f, -0.018f);
            postal18.localScale = new Vector3(0.7f, 0.7f, 1f);
            Transform postal19 = Box("Ink stroke", postal18, new Vector3(0.0f, 0.08f, 0f), new Vector3(0.05f, 0.012f, 0.003f), postalInk);
            Transform postal20 = Box("Ink stroke", postal18, new Vector3(0.0375f, 0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal20.localRotation = new Quaternion(0f, 0f, -0.4241554f, 0.9055894f);
            Transform postal21 = Box("Ink stroke", postal18, new Vector3(0.05f, 0.0f, 0f), new Vector3(0.1f, 0.012f, 0.003f), postalInk);
            postal21.localRotation = new Quaternion(0f, 0f, -0.7071068f, 0.7071068f);
            Transform postal22 = Box("Ink stroke", postal18, new Vector3(0.0375f, -0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal22.localRotation = new Quaternion(0f, 0f, -0.9055894f, 0.4241554f);
            Transform postal23 = Box("Ink stroke", postal18, new Vector3(0.0f, -0.08f, 0f), new Vector3(0.05f, 0.012f, 0.003f), postalInk);
            postal23.localRotation = new Quaternion(0f, 0f, 1.0f, 0.0f);
            Transform postal24 = Box("Ink stroke", postal18, new Vector3(-0.0375f, -0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal24.localRotation = new Quaternion(0f, 0f, 0.9055894f, 0.4241554f);
            Transform postal25 = Box("Ink stroke", postal18, new Vector3(-0.05f, 0.0f, 0f), new Vector3(0.1f, 0.012f, 0.003f), postalInk);
            postal25.localRotation = new Quaternion(0f, 0f, 0.7071068f, 0.7071068f);
            Transform postal26 = Box("Ink stroke", postal18, new Vector3(-0.0375f, 0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal26.localRotation = new Quaternion(0f, 0f, 0.4241554f, 0.9055894f);
            Transform postal27 = Root("ZIP digit 5", postal0);
            postal27.localPosition = new Vector3(0.22f, -0.12f, -0.018f);
            postal27.localScale = new Vector3(0.7f, 0.7f, 1f);
            Transform postal28 = Box("Ink stroke", postal27, new Vector3(0.0f, 0.08f, 0f), new Vector3(0.1f, 0.012f, 0.003f), postalInk);
            postal28.localRotation = new Quaternion(0f, 0f, 1.0f, 0.0f);
            Transform postal29 = Box("Ink stroke", postal27, new Vector3(-0.05f, 0.0425f, 0f), new Vector3(0.075f, 0.012f, 0.003f), postalInk);
            postal29.localRotation = new Quaternion(0f, 0f, -0.7071068f, 0.7071068f);
            Transform postal30 = Box("Ink stroke", postal27, new Vector3(-0.0125f, 0.005f, 0f), new Vector3(0.075f, 0.012f, 0.003f), postalInk);
            Transform postal31 = Box("Ink stroke", postal27, new Vector3(0.0375f, -0.0075f, 0f), new Vector3(0.0353553f, 0.012f, 0.003f), postalInk);
            postal31.localRotation = new Quaternion(0f, 0f, -0.3826834f, 0.9238795f);
            Transform postal32 = Box("Ink stroke", postal27, new Vector3(0.05f, -0.035f, 0f), new Vector3(0.03f, 0.012f, 0.003f), postalInk);
            postal32.localRotation = new Quaternion(0f, 0f, -0.7071068f, 0.7071068f);
            Transform postal33 = Box("Ink stroke", postal27, new Vector3(0.0375f, -0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal33.localRotation = new Quaternion(0f, 0f, -0.9055894f, 0.4241554f);
            Transform postal34 = Box("Ink stroke", postal27, new Vector3(-0.0125f, -0.08f, 0f), new Vector3(0.075f, 0.012f, 0.003f), postalInk);
            postal34.localRotation = new Quaternion(0f, 0f, 1.0f, 0.0f);
            Transform postal35 = Root("ZIP digit 0", postal0);
            postal35.localPosition = new Vector3(0.31f, -0.12f, -0.018f);
            postal35.localScale = new Vector3(0.7f, 0.7f, 1f);
            Transform postal36 = Box("Ink stroke", postal35, new Vector3(0.0f, 0.08f, 0f), new Vector3(0.05f, 0.012f, 0.003f), postalInk);
            Transform postal37 = Box("Ink stroke", postal35, new Vector3(0.0375f, 0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal37.localRotation = new Quaternion(0f, 0f, -0.4241554f, 0.9055894f);
            Transform postal38 = Box("Ink stroke", postal35, new Vector3(0.05f, 0.0f, 0f), new Vector3(0.1f, 0.012f, 0.003f), postalInk);
            postal38.localRotation = new Quaternion(0f, 0f, -0.7071068f, 0.7071068f);
            Transform postal39 = Box("Ink stroke", postal35, new Vector3(0.0375f, -0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal39.localRotation = new Quaternion(0f, 0f, -0.9055894f, 0.4241554f);
            Transform postal40 = Box("Ink stroke", postal35, new Vector3(0.0f, -0.08f, 0f), new Vector3(0.05f, 0.012f, 0.003f), postalInk);
            postal40.localRotation = new Quaternion(0f, 0f, 1.0f, 0.0f);
            Transform postal41 = Box("Ink stroke", postal35, new Vector3(-0.0375f, -0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal41.localRotation = new Quaternion(0f, 0f, 0.9055894f, 0.4241554f);
            Transform postal42 = Box("Ink stroke", postal35, new Vector3(-0.05f, 0.0f, 0f), new Vector3(0.1f, 0.012f, 0.003f), postalInk);
            postal42.localRotation = new Quaternion(0f, 0f, 0.7071068f, 0.7071068f);
            Transform postal43 = Box("Ink stroke", postal35, new Vector3(-0.0375f, 0.065f, 0f), new Vector3(0.0390512f, 0.012f, 0.003f), postalInk);
            postal43.localRotation = new Quaternion(0f, 0f, 0.4241554f, 0.9055894f);
            Transform postal44 = Root("ZIP digit 1", postal0);
            postal44.localPosition = new Vector3(0.4f, -0.12f, -0.018f);
            postal44.localScale = new Vector3(0.7f, 0.7f, 1f);
            Transform postal45 = Box("Ink stroke", postal44, new Vector3(-0.0075f, 0.065f, 0f), new Vector3(0.0460977f, 0.012f, 0.003f), postalInk);
            postal45.localRotation = new Quaternion(0f, 0f, 0.3469462f, 0.937885f);
            Transform postal46 = Box("Ink stroke", postal44, new Vector3(0.01f, 0.0f, 0f), new Vector3(0.16f, 0.012f, 0.003f), postalInk);
            postal46.localRotation = new Quaternion(0f, 0f, -0.7071068f, 0.7071068f);
            Transform postal47 = Box("Ink stroke", postal44, new Vector3(0.01f, -0.08f, 0f), new Vector3(0.08f, 0.012f, 0.003f), postalInk);
            Transform postal48 = Root("Moving postal stamp", postal0);
            postal48.localPosition = new Vector3(-0.28f, 0.1f, -0.38f);
            postal48.localScale = new Vector3(1f, 1f, 1f);
            Transform postal49 = Box("Stamp rubber", postal48, new Vector3(0f, 0f, 0f), new Vector3(0.24f, 0.105f, 0.035f), postalInk);
            Transform postal50 = Box("Stamp brass plate", postal48, new Vector3(0f, 0f, -0.028f), new Vector3(0.25f, 0.115f, 0.023f), postalBrass);
            Transform postal51 = Box("Stamp wooden handle", postal48, new Vector3(0f, 0f, -0.1f), new Vector3(0.08f, 0.07f, 0.13f), postalOak);
            Transform postal52 = Box("Stamp grip", postal48, new Vector3(0f, 0f, -0.18f), new Vector3(0.18f, 0.1f, 0.055f), postalOak);
            Transform postal53 = Root("Postal cancellation ink", postal0);
            postal53.localPosition = new Vector3(-0.28f, 0.1f, -0.014f);
            postal53.localScale = new Vector3(1f, 1f, 1f);
            postal53.gameObject.SetActive(false);
            Transform postal54 = Root("ZIP recognition outline", postal0);
            postal54.localPosition = new Vector3(0.04f, -0.12f, -0.023f);
            postal54.localScale = new Vector3(1f, 1f, 1f);
            postal54.gameObject.SetActive(false);
            Transform postal55 = Box("Postal ink border", postal53, new Vector3(0.0f, -0.048f, 0f), new Vector3(0.23f, 0.006f, 0.003f), postalBlue);
            Transform postal56 = Box("Postal ink border", postal53, new Vector3(0.0f, 0.048f, 0f), new Vector3(0.23f, 0.006f, 0.003f), postalBlue);
            Transform postal57 = Box("Postal ink border", postal53, new Vector3(-0.115f, 0.0f, 0f), new Vector3(0.096f, 0.006f, 0.003f), postalBlue);
            postal57.localRotation = new Quaternion(0f, 0f, 0.7071068f, 0.7071068f);
            Transform postal58 = Box("Postal ink border", postal53, new Vector3(0.115f, 0.0f, 0f), new Vector3(0.096f, 0.006f, 0.003f), postalBlue);
            postal58.localRotation = new Quaternion(0f, 0f, 0.7071068f, 0.7071068f);
            Transform postal59 = Box("Postal ink border", postal54, new Vector3(0.0f, -0.075f, 0f), new Vector3(0.104f, 0.004f, 0.003f), postalBlue);
            Transform postal60 = Box("Postal ink border", postal54, new Vector3(0.0f, 0.075f, 0f), new Vector3(0.104f, 0.004f, 0.003f), postalBlue);
            Transform postal61 = Box("Postal ink border", postal54, new Vector3(-0.052f, 0.0f, 0f), new Vector3(0.15f, 0.004f, 0.003f), postalBlue);
            postal61.localRotation = new Quaternion(0f, 0f, 0.7071068f, 0.7071068f);
            Transform postal62 = Box("Postal ink border", postal54, new Vector3(0.052f, 0.0f, 0f), new Vector3(0.15f, 0.004f, 0.003f), postalBlue);
            postal62.localRotation = new Quaternion(0f, 0f, 0.7071068f, 0.7071068f);
            Transform postal63 = Box("Cancellation ink line", postal53, new Vector3(0f, -0.025f, 0f), new Vector3(0.17f, 0.005f, 0.003f), postalBlue);
            Transform postal64 = Box("Cancellation ink line", postal53, new Vector3(0f, 0f, 0f), new Vector3(0.17f, 0.005f, 0.003f), postalBlue);
            Transform postal65 = Box("Cancellation ink line", postal53, new Vector3(0f, 0.025f, 0f), new Vector3(0.17f, 0.005f, 0.003f), postalBlue);
            Assign(world, "_postalEnvelope", postal0);
            Assign(world, "_postalStamp", postal48);
            Assign(world, "_postmark", postal53);
            Assign(world, "_postalOutline", postal54);
        }

        private static Transform BuildPostOffice(Transform stage, MonoBehaviour world)
        {
            Transform room = Root("Post office interior", stage);
            Material plaster = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficePlaster.mat");
            Material floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeFloor.mat");
            Material oak = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeOak.mat");
            Material brass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeBrass.mat");
            Material paper = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficePaper.mat");
            Material blue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeBlue.mat");
            Material light = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeLight.mat");
            Material ink = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CNNArchiveOpening/PostOfficeInk.mat");
            Box("Lobby floor", room, new Vector3(0.0f, -0.55f, 1.0f), new Vector3(9.0f, 0.12f, 10.0f), floor);
            Box("Ceiling", room, new Vector3(0.0f, 3.25f, 1.0f), new Vector3(9.0f, 0.15f, 10.0f), plaster);
            Box("Rear wall", room, new Vector3(0.0f, 1.35f, -4.0f), new Vector3(9.0f, 3.8f, 0.18f), plaster);
            Box("Sorting room wall", room, new Vector3(0.0f, 1.35f, 6.0f), new Vector3(9.0f, 3.8f, 0.18f), plaster);
            Box("Side wall", room, new Vector3(-4.5f, 1.35f, 1.0f), new Vector3(0.18f, 3.8f, 10.0f), plaster);
            Box("Oak skirting", room, new Vector3(-4.4325f, -0.1f, 1.0f), new Vector3(0.12f, 0.7f, 10.0f), oak);
            Box("Window frame", room, new Vector3(-4.365f, 1.65f, -2.0f), new Vector3(0.12f, 1.8f, 1.8f), oak);
            Box("Daylight window", room, new Vector3(-4.2749999999999995f, 1.65f, -2.0f), new Vector3(0.06f, 1.6f, 1.6f), light);
            Box("Window mullion", room, new Vector3(-4.2299999999999995f, 1.65f, -2.0f), new Vector3(0.08f, 1.6f, 0.05f), brass);
            Box("Window frame", room, new Vector3(-4.365f, 1.65f, 1.0f), new Vector3(0.12f, 1.8f, 1.8f), oak);
            Box("Daylight window", room, new Vector3(-4.2749999999999995f, 1.65f, 1.0f), new Vector3(0.06f, 1.6f, 1.6f), light);
            Box("Window mullion", room, new Vector3(-4.2299999999999995f, 1.65f, 1.0f), new Vector3(0.08f, 1.6f, 0.05f), brass);
            Box("Window frame", room, new Vector3(-4.365f, 1.65f, 4.0f), new Vector3(0.12f, 1.8f, 1.8f), oak);
            Box("Daylight window", room, new Vector3(-4.2749999999999995f, 1.65f, 4.0f), new Vector3(0.06f, 1.6f, 1.6f), light);
            Box("Window mullion", room, new Vector3(-4.2299999999999995f, 1.65f, 4.0f), new Vector3(0.08f, 1.6f, 0.05f), brass);
            Box("Side wall", room, new Vector3(4.5f, 1.35f, 1.0f), new Vector3(0.18f, 3.8f, 10.0f), plaster);
            Box("Oak skirting", room, new Vector3(4.4325f, -0.1f, 1.0f), new Vector3(0.12f, 0.7f, 10.0f), oak);
            Box("Window frame", room, new Vector3(4.365f, 1.65f, -2.0f), new Vector3(0.12f, 1.8f, 1.8f), oak);
            Box("Daylight window", room, new Vector3(4.2749999999999995f, 1.65f, -2.0f), new Vector3(0.06f, 1.6f, 1.6f), light);
            Box("Window mullion", room, new Vector3(4.2299999999999995f, 1.65f, -2.0f), new Vector3(0.08f, 1.6f, 0.05f), brass);
            Box("Window frame", room, new Vector3(4.365f, 1.65f, 1.0f), new Vector3(0.12f, 1.8f, 1.8f), oak);
            Box("Daylight window", room, new Vector3(4.2749999999999995f, 1.65f, 1.0f), new Vector3(0.06f, 1.6f, 1.6f), light);
            Box("Window mullion", room, new Vector3(4.2299999999999995f, 1.65f, 1.0f), new Vector3(0.08f, 1.6f, 0.05f), brass);
            Box("Window frame", room, new Vector3(4.365f, 1.65f, 4.0f), new Vector3(0.12f, 1.8f, 1.8f), oak);
            Box("Daylight window", room, new Vector3(4.2749999999999995f, 1.65f, 4.0f), new Vector3(0.06f, 1.6f, 1.6f), light);
            Box("Window mullion", room, new Vector3(4.2299999999999995f, 1.65f, 4.0f), new Vector3(0.08f, 1.6f, 0.05f), brass);
            Box("Counter cabinet", room, new Vector3(-2.7f, 0.05f, 2.1f), new Vector3(2.5f, 1.1f, 0.85f), oak);
            Box("Counter top", room, new Vector3(-2.7f, 0.64f, 2.04f), new Vector3(2.6f, 0.12f, 1.05f), ink);
            Box("Counter panel", room, new Vector3(-2.7f, 0.12f, 1.66f), new Vector3(2.25f, 0.72f, 0.035f), brass);
            Box("Counter inset", room, new Vector3(-2.7f, 0.12f, 1.63f), new Vector3(2.12f, 0.61f, 0.025f), oak);
            Box("Service window post", room, new Vector3(-3.9000000000000004f, 1.65f, 2.35f), new Vector3(0.07f, 2.05f, 0.07f), brass);
            Box("Service window post", room, new Vector3(-1.5000000000000002f, 1.65f, 2.35f), new Vector3(0.07f, 2.05f, 0.07f), brass);
            Box("Service window lintel", room, new Vector3(-2.7f, 2.65f, 2.35f), new Vector3(2.5f, 0.14f, 0.1f), oak);
            Box("Mail tray", room, new Vector3(-3.35f, 0.74f, 2.05f), new Vector3(0.5f, 0.08f, 0.32f), blue);
            Box("Letters in tray", room, new Vector3(-3.35f, 0.79f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(-3.35f, 0.803f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(-3.35f, 0.8160000000000001f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(-3.35f, 0.8290000000000001f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Envelope on counter", room, new Vector3(-2.45f, 0.71f, 1.85f), new Vector3(0.32f, 0.018f, 0.2f), paper);
            Box("Envelope stamp", room, new Vector3(-2.3400000000000003f, 0.723f, 1.9f), new Vector3(0.045f, 0.006f, 0.045f), blue);
            Box("Rubber stamp base", room, new Vector3(-2.0500000000000003f, 0.75f, 2.1f), new Vector3(0.17f, 0.08f, 0.11f), ink);
            Box("Rubber stamp handle", room, new Vector3(-2.0500000000000003f, 0.86f, 2.1f), new Vector3(0.065f, 0.16f, 0.065f), oak);
            Box("Counter cabinet", room, new Vector3(0.0f, 0.05f, 2.1f), new Vector3(2.5f, 1.1f, 0.85f), oak);
            Box("Counter top", room, new Vector3(0.0f, 0.64f, 2.04f), new Vector3(2.6f, 0.12f, 1.05f), ink);
            Box("Counter panel", room, new Vector3(0.0f, 0.12f, 1.66f), new Vector3(2.25f, 0.72f, 0.035f), brass);
            Box("Counter inset", room, new Vector3(0.0f, 0.12f, 1.63f), new Vector3(2.12f, 0.61f, 0.025f), oak);
            Box("Service window post", room, new Vector3(-1.2f, 1.65f, 2.35f), new Vector3(0.07f, 2.05f, 0.07f), brass);
            Box("Service window post", room, new Vector3(1.2f, 1.65f, 2.35f), new Vector3(0.07f, 2.05f, 0.07f), brass);
            Box("Service window lintel", room, new Vector3(0.0f, 2.65f, 2.35f), new Vector3(2.5f, 0.14f, 0.1f), oak);
            Box("Mail tray", room, new Vector3(-0.65f, 0.74f, 2.05f), new Vector3(0.5f, 0.08f, 0.32f), blue);
            Box("Letters in tray", room, new Vector3(-0.65f, 0.79f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(-0.65f, 0.803f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(-0.65f, 0.8160000000000001f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(-0.65f, 0.8290000000000001f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Counter cabinet", room, new Vector3(2.7f, 0.05f, 2.1f), new Vector3(2.5f, 1.1f, 0.85f), oak);
            Box("Counter top", room, new Vector3(2.7f, 0.64f, 2.04f), new Vector3(2.6f, 0.12f, 1.05f), ink);
            Box("Counter panel", room, new Vector3(2.7f, 0.12f, 1.66f), new Vector3(2.25f, 0.72f, 0.035f), brass);
            Box("Counter inset", room, new Vector3(2.7f, 0.12f, 1.63f), new Vector3(2.12f, 0.61f, 0.025f), oak);
            Box("Service window post", room, new Vector3(1.5000000000000002f, 1.65f, 2.35f), new Vector3(0.07f, 2.05f, 0.07f), brass);
            Box("Service window post", room, new Vector3(3.9000000000000004f, 1.65f, 2.35f), new Vector3(0.07f, 2.05f, 0.07f), brass);
            Box("Service window lintel", room, new Vector3(2.7f, 2.65f, 2.35f), new Vector3(2.5f, 0.14f, 0.1f), oak);
            Box("Mail tray", room, new Vector3(2.0500000000000003f, 0.74f, 2.05f), new Vector3(0.5f, 0.08f, 0.32f), blue);
            Box("Letters in tray", room, new Vector3(2.0500000000000003f, 0.79f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(2.0500000000000003f, 0.803f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(2.0500000000000003f, 0.8160000000000001f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Letters in tray", room, new Vector3(2.0500000000000003f, 0.8290000000000001f, 2.05f), new Vector3(0.38f, 0.012f, 0.23f), paper);
            Box("Envelope on counter", room, new Vector3(2.95f, 0.71f, 1.85f), new Vector3(0.32f, 0.018f, 0.2f), paper);
            Box("Envelope stamp", room, new Vector3(3.06f, 0.723f, 1.9f), new Vector3(0.045f, 0.006f, 0.045f), blue);
            Box("Rubber stamp base", room, new Vector3(3.35f, 0.75f, 2.1f), new Vector3(0.17f, 0.08f, 0.11f), ink);
            Box("Rubber stamp handle", room, new Vector3(3.35f, 0.86f, 2.1f), new Vector3(0.065f, 0.16f, 0.065f), oak);
            Box("Sorting shelf backing", room, new Vector3(0.0f, 1.25f, 5.55f), new Vector3(6.8f, 2.8f, 0.18f), oak);
            Box("Sorting shelf", room, new Vector3(0.0f, 0.0f, 5.25f), new Vector3(6.8f, 0.055f, 0.65f), brass);
            Box("Sorting shelf", room, new Vector3(0.0f, 0.55f, 5.25f), new Vector3(6.8f, 0.055f, 0.65f), brass);
            Box("Sorting shelf", room, new Vector3(0.0f, 1.1f, 5.25f), new Vector3(6.8f, 0.055f, 0.65f), brass);
            Box("Sorting shelf", room, new Vector3(0.0f, 1.65f, 5.25f), new Vector3(6.8f, 0.055f, 0.65f), brass);
            Box("Sorting shelf", room, new Vector3(0.0f, 2.2f, 5.25f), new Vector3(6.8f, 0.055f, 0.65f), brass);
            Box("Sorting shelf", room, new Vector3(0.0f, 2.65f, 5.25f), new Vector3(6.8f, 0.055f, 0.65f), brass);
            Box("Sorting divider", room, new Vector3(-3.4f, 1.3f, 5.25f), new Vector3(0.05f, 2.7f, 0.65f), oak);
            Box("Sorting divider", room, new Vector3(-2.25f, 1.3f, 5.25f), new Vector3(0.05f, 2.7f, 0.65f), oak);
            Box("Sorting divider", room, new Vector3(-1.1f, 1.3f, 5.25f), new Vector3(0.05f, 2.7f, 0.65f), oak);
            Box("Sorting divider", room, new Vector3(0.0f, 1.3f, 5.25f), new Vector3(0.05f, 2.7f, 0.65f), oak);
            Box("Sorting divider", room, new Vector3(1.1f, 1.3f, 5.25f), new Vector3(0.05f, 2.7f, 0.65f), oak);
            Box("Sorting divider", room, new Vector3(2.25f, 1.3f, 5.25f), new Vector3(0.05f, 2.7f, 0.65f), oak);
            Box("Sorting divider", room, new Vector3(3.4f, 1.3f, 5.25f), new Vector3(0.05f, 2.7f, 0.65f), oak);
            Box("Sorted mail bundle", room, new Vector3(-2.85f, 0.09f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-1.7200000000000002f, 0.09f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-0.5900000000000003f, 0.09f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(0.5399999999999996f, 0.09f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(1.6699999999999995f, 0.09f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(2.7999999999999994f, 0.09f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-2.85f, 0.64f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-1.7200000000000002f, 0.64f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-0.5900000000000003f, 0.64f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(0.5399999999999996f, 0.64f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(1.6699999999999995f, 0.64f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(2.7999999999999994f, 0.64f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-2.85f, 1.1900000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-1.7200000000000002f, 1.1900000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-0.5900000000000003f, 1.1900000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(0.5399999999999996f, 1.1900000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(1.6699999999999995f, 1.1900000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(2.7999999999999994f, 1.1900000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-2.85f, 1.7400000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-1.7200000000000002f, 1.7400000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(-0.5900000000000003f, 1.7400000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(0.5399999999999996f, 1.7400000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(1.6699999999999995f, 1.7400000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Sorted mail bundle", room, new Vector3(2.7999999999999994f, 1.7400000000000002f, 5.22f), new Vector3(0.65f, 0.13f, 0.32f), paper);
            Box("Writing desk", room, new Vector3(-2.8f, 0.55f, -1.5f), new Vector3(1.5f, 0.1f, 0.65f), oak);
            Box("Desk leg", room, new Vector3(-3.4f, 0.02f, -1.5f), new Vector3(0.09f, 1.0f, 0.5f), ink);
            Box("Desk leg", room, new Vector3(-2.1999999999999997f, 0.02f, -1.5f), new Vector3(0.09f, 1.0f, 0.5f), ink);
            Box("Parcel", room, new Vector3(-2.8f, 0.8f, -1.5f), new Vector3(0.48f, 0.4f, 0.36f), paper);
            Box("Parcel tape", room, new Vector3(-2.8f, 0.805f, -1.5f), new Vector3(0.07f, 0.41f, 0.37f), brass);
            Box("Writing desk", room, new Vector3(2.8f, 0.55f, -1.5f), new Vector3(1.5f, 0.1f, 0.65f), oak);
            Box("Desk leg", room, new Vector3(2.1999999999999997f, 0.02f, -1.5f), new Vector3(0.09f, 1.0f, 0.5f), ink);
            Box("Desk leg", room, new Vector3(3.4f, 0.02f, -1.5f), new Vector3(0.09f, 1.0f, 0.5f), ink);
            Box("Parcel", room, new Vector3(2.8f, 0.8f, -1.5f), new Vector3(0.48f, 0.4f, 0.36f), paper);
            Box("Parcel tape", room, new Vector3(2.8f, 0.805f, -1.5f), new Vector3(0.07f, 0.41f, 0.37f), brass);
            Box("Ceiling light", room, new Vector3(-2.5f, 3.12f, 1.0f), new Vector3(1.3f, 0.08f, 0.35f), light);
            Box("Ceiling light", room, new Vector3(0.0f, 3.12f, 1.0f), new Vector3(1.3f, 0.08f, 0.35f), light);
            Box("Ceiling light", room, new Vector3(2.5f, 3.12f, 1.0f), new Vector3(1.3f, 0.08f, 0.35f), light);
            BuildPostOfficeDetails(room, oak, brass, paper, ink, blue);
            Assign(world, "_crowd", BuildPostOfficeCrowd(room));
            room.gameObject.SetActive(false);
            return room;
        }

        private static void BuildOpeningEntrance(Transform factory, MonoBehaviour world)
        {
            BuildOpeningFacadeDetails(factory);
            Material concrete = FactoryMaterial("DarkConcrete");
            Material steel = FactoryMaterial("AgedSteel");
            Material shutter = FactoryMaterial("GalvanizedShutter");
            Material sign = FactoryMaterial("RoomSignSurface");
            Material floor = FactoryMaterial("SealedConcreteFloor");
            Material grate = FactoryMaterial("AgedSteel");
            Material amber = FactoryMaterial("SafetyYellow");

            Box("Left concrete wing", factory, new Vector3(-3.42f, 1.82f, .48f), new Vector3(3.64f, 4.24f, 1.56f), FactoryMaterial("FactoryLabPanelSlate"));
            Box("Right concrete wing", factory, new Vector3(3.42f, 1.82f, .48f), new Vector3(3.64f, 4.24f, 1.56f), FactoryMaterial("FactoryLabPanelSlate"));
            Box("Concrete lintel", factory, new Vector3(0, 3.75f, .48f), new Vector3(3.2f, 1.1f, 1.56f), concrete);
            Box("Entrance canopy", factory, new Vector3(0, 3.16f, -.93f), new Vector3(6.1f, .23f, 1.64f), steel);
            Box("Canopy signal strip", factory, new Vector3(0, 3.16f, -1.758f), new Vector3(4.8f, .036f, .022f), FactoryMaterial("ArchiveCyan"));
            Box("Sign frame", factory, new Vector3(0, 4.06f, -.46f), new Vector3(5.6f, .9f, .28f), steel);
            Box("Sign face", factory, new Vector3(0, 4.06f, -.617f), new Vector3(5.36f, .72f, .036f), sign);

            Box("Approach and tunnel floor", factory, new Vector3(0, -.43f, -2.45f), new Vector3(5.6f, .26f, 14.7f), floor);
            Box("Recessed threshold", factory, new Vector3(0, -.285f, .08f), new Vector3(3.2f, .03f, .68f), grate);
            Box("Threshold safety strip", factory, new Vector3(0, -.28f, -.27f), new Vector3(3.14f, .032f, .065f), amber);
            Box("Tunnel end wall", factory, new Vector3(0, 1.34f, 4.9f), new Vector3(3.52f, 3.28f, .25f), concrete);
            Box("Tunnel ceiling", factory, new Vector3(0, 3.01f, 2.77f), new Vector3(3.56f, .22f, 4.35f), concrete);
            Box("Tunnel end equipment plate", factory, new Vector3(0, 1.32f, 4.735f), new Vector3(1.32f, 1.55f, .06f), sign);
            Box("Tunnel end indicator", factory, new Vector3(0, 1.67f, 4.697f), new Vector3(.77f, .065f, .026f), FactoryMaterial("ArchiveCyan"));

            Canvas titleCanvas = CanvasAt(factory, "Factory title", new Vector3(0, 4.06f, -.65f), new Vector2(1320, 180), .004f);
            Color signLettering = new Color(.94f, .97f, .96f);
            Text title = TextAt(titleCanvas.transform, "Title", "CNN FACTORY", new Vector2(0, 34), new Vector2(1280, 110), 100);
            title.color = signLettering;
            Text entrance = TextAt(titleCanvas.transform, "Entrance label", "LEARN  /  BUILD  /  EXPLORE", new Vector2(0, -48), new Vector2(1000, 70), 36);
            entrance.color = new Color(.45f, .76f, .77f);

            for (int side = -1; side <= 1; side += 2)
            {
                Box("Steel door jamb", factory, new Vector3(side * 1.71f, 1.34f, -.48f), new Vector3(.31f, 3.28f, .55f), steel);
                Box("Jamb signal channel", factory, new Vector3(side * 1.72f, 1.72f, -.777f), new Vector3(.135f, 1.88f, .052f), sign);
                Box("Jamb signal", factory, new Vector3(side * 1.72f, 1.72f, -.812f), new Vector3(.046f, 1.6f, .022f), FactoryMaterial("ArchiveCyan"));
                Box("Jamb impact guard", factory, new Vector3(side * 1.72f, .085f, -.775f), new Vector3(.27f, .57f, .17f), amber);
                Box("Facade stiffener", factory, new Vector3(side * 4.64f, 1.76f, -.43f), new Vector3(.22f, 4.12f, .29f), steel);
                Box("Canopy bracket", factory, new Vector3(side * 2.74f, 2.91f, -.93f), new Vector3(.26f, .28f, 1.28f), steel);
                Box("Tunnel side wall", factory, new Vector3(side * 1.73f, 1.32f, 2.94f), new Vector3(.26f, 3.24f, 3.95f), concrete);
                Box("Guide rail bed", factory, new Vector3(side * .99f, -.29f, -4.82f), new Vector3(.14f, .046f, 9.1f), steel);
                Box("Recessed guide rail", factory, new Vector3(side * .99f, -.264f, -4.82f), new Vector3(.036f, .018f, 8.96f), FactoryMaterial("ArchiveCyan"));

                Transform door = Root(side < 0 ? "Left door" : "Right door", factory);
                door.localPosition = new Vector3(side * .8f, 1.3f, .12f);
                Box("Shutter panel", door, Vector3.zero, new Vector3(1.59f, 3.2f, .15f), shutter);
                for (int i = 0; i < 9; i++)
                {
                    Box("Pressed shutter rib", door, new Vector3(0, -1.36f + i * .34f, -.11f), new Vector3(1.55f, .065f, .085f), shutter);
                }
                Box("Shutter leading edge", door, new Vector3(-side * .75f, 0, -.112f), new Vector3(.095f, 3.17f, .09f), steel);
                Box("Shutter safety plate", door, new Vector3(0, -1.41f, -.16f), new Vector3(1.38f, .1f, .035f), amber);
                BuildOpeningDoorDetails(door, side);
                Assign(world, side < 0 ? "_leftDoor" : "_rightDoor", door);

                for (int i = 0; i < 5; i++)
                {
                    Transform chevron = Box("Route chevron", factory, new Vector3(side * .215f, -.278f, -8.4f + i * 1.7f), new Vector3(.51f, .028f, .082f), FactoryMaterial("ArchiveCyan"));
                    chevron.localRotation = Quaternion.Euler(0, side * 32f, 0);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float z = 1.35f + i * 1.35f;
                Box("Tunnel lamp housing", factory, new Vector3(0, 2.87f, z), new Vector3(1.32f, .16f, .26f), steel);
                Box("Tunnel lamp diffuser", factory, new Vector3(0, 2.775f, z), new Vector3(1.14f, .045f, .13f), FactoryMaterial("ArchiveCyan"));
            }
        }

        private static void BuildOpeningFacadeDetails(Transform factory)
        {
            Material amber = FactoryMaterial("SafetyYellow");
            Material cream = FactoryMaterial("FactoryLabPanelCream");
            Material cyan = FactoryMaterial("ArchiveCyan");
            Material dark = FactoryMaterial("ArchiveCharcoal");
            Material slate = FactoryMaterial("FactoryLabPanelSlate");
            Material steel = FactoryMaterial("AgedSteel");
            Box("Roof perimeter cap", factory, new Vector3(0f, 4.39f, 0.12f), new Vector3(10.65f, 0.18f, 2.1f), steel);
            Box("Roof reveal shadow", factory, new Vector3(0f, 4.25f, -0.355f), new Vector3(10.3f, 0.07f, 0.12f), dark);
            Box("Canopy fascia", factory, new Vector3(0f, 3.16f, -1.78f), new Vector3(6.25f, 0.25f, 0.09f), dark);
            Box("Canopy upper lip", factory, new Vector3(0f, 3.31f, -1.75f), new Vector3(6.32f, 0.055f, 0.16f), steel);
            Box("Header lower trim", factory, new Vector3(0f, 3.57f, -0.65f), new Vector3(5.58f, 0.055f, 0.1f), steel);
            Box("Sign accent rule", factory, new Vector3(0f, 3.69f, -0.65f), new Vector3(4.8f, 0.018f, 0.02f), cyan);
            Box("Entry drain", factory, new Vector3(0f, -0.285f, -0.95f), new Vector3(3.25f, 0.025f, 0.32f), steel);
            Box("Facade inset shadow", factory, new Vector3(-3.44f, 1.75f, -0.331f), new Vector3(2.95f, 3.9f, 0.08f), dark);
            Box("Architectural cladding", factory, new Vector3(-2.49f, 1.77f, -0.39f), new Vector3(0.91f, 3.73f, 0.12f), cream);
            Box("Cladding lower plinth", factory, new Vector3(-2.49f, -0.05f, -0.49f), new Vector3(0.91f, 0.27f, 0.16f), steel);
            Box("Architectural cladding", factory, new Vector3(-3.44f, 1.77f, -0.39f), new Vector3(0.91f, 3.73f, 0.12f), cream);
            Box("Cladding lower plinth", factory, new Vector3(-3.44f, -0.05f, -0.49f), new Vector3(0.91f, 0.27f, 0.16f), steel);
            Box("Architectural cladding", factory, new Vector3(-4.39f, 1.77f, -0.39f), new Vector3(0.91f, 3.73f, 0.12f), cream);
            Box("Cladding lower plinth", factory, new Vector3(-4.39f, -0.05f, -0.49f), new Vector3(0.91f, 0.27f, 0.16f), steel);
            Box("Observation window frame", factory, new Vector3(-3.44f, 2.5f, -0.49f), new Vector3(2.54f, 1.13f, 0.2f), steel);
            Box("Observation window recess", factory, new Vector3(-3.44f, 2.5f, -0.603f), new Vector3(2.36f, 0.95f, 0.045f), dark);
            Box("Window interior light", factory, new Vector3(-4.3025f, 2.69f, -0.635f), new Vector3(0.52f, 0.045f, 0.012f), cyan);
            Box("Window lower reflection", factory, new Vector3(-4.3025f, 2.24f, -0.635f), new Vector3(0.52f, 0.016f, 0.012f), cyan);
            Box("Window mullion", factory, new Vector3(-4.015f, 2.5f, -0.66f), new Vector3(0.026f, 0.94f, 0.065f), steel);
            Box("Window interior light", factory, new Vector3(-3.7275f, 2.69f, -0.635f), new Vector3(0.52f, 0.045f, 0.012f), cyan);
            Box("Window lower reflection", factory, new Vector3(-3.7275f, 2.24f, -0.635f), new Vector3(0.52f, 0.016f, 0.012f), cyan);
            Box("Window mullion", factory, new Vector3(-3.44f, 2.5f, -0.66f), new Vector3(0.026f, 0.94f, 0.065f), steel);
            Box("Window interior light", factory, new Vector3(-3.1525f, 2.69f, -0.635f), new Vector3(0.52f, 0.045f, 0.012f), cyan);
            Box("Window lower reflection", factory, new Vector3(-3.1525f, 2.24f, -0.635f), new Vector3(0.52f, 0.016f, 0.012f), cyan);
            Box("Window mullion", factory, new Vector3(-2.865f, 2.5f, -0.66f), new Vector3(0.026f, 0.94f, 0.065f), steel);
            Box("Window interior light", factory, new Vector3(-2.5775f, 2.69f, -0.635f), new Vector3(0.52f, 0.045f, 0.012f), cyan);
            Box("Window lower reflection", factory, new Vector3(-2.5775f, 2.24f, -0.635f), new Vector3(0.52f, 0.016f, 0.012f), cyan);
            Box("Window sill", factory, new Vector3(-3.44f, 1.99f, -0.64f), new Vector3(2.66f, 0.075f, 0.38f), steel);
            Box("Vent surround", factory, new Vector3(-3.44f, 0.93f, -0.49f), new Vector3(2.08f, 0.9f, 0.18f), steel);
            Box("Vent recess", factory, new Vector3(-3.44f, 0.93f, -0.59f), new Vector3(1.91f, 0.73f, 0.04f), dark);
            Box("Vent horizontal louvre", factory, new Vector3(-3.44f, 0.65f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(-3.44f, 0.74f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(-3.44f, 0.83f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(-3.44f, 0.92f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(-3.44f, 1.01f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(-3.44f, 1.1f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(-3.44f, 1.19f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent captive fastener", factory, new Vector3(-4.35f, 0.58f, -0.65f), new Vector3(0.038f, 0.038f, 0.018f), cream);
            Box("Vent captive fastener", factory, new Vector3(-4.35f, 1.28f, -0.65f), new Vector3(0.038f, 0.038f, 0.018f), cream);
            Box("Vent captive fastener", factory, new Vector3(-2.53f, 0.58f, -0.65f), new Vector3(0.038f, 0.038f, 0.018f), cream);
            Box("Vent captive fastener", factory, new Vector3(-2.53f, 1.28f, -0.65f), new Vector3(0.038f, 0.038f, 0.018f), cream);
            Box("Exterior service riser", factory, new Vector3(-4.95f, 1.64f, -0.54f), new Vector3(0.065f, 3.65f, 0.075f), steel);
            Box("Riser clamp", factory, new Vector3(-4.95f, 0.2f, -0.59f), new Vector3(0.15f, 0.05f, 0.12f), steel);
            Box("Riser clamp", factory, new Vector3(-4.95f, 1.35f, -0.59f), new Vector3(0.15f, 0.05f, 0.12f), steel);
            Box("Riser clamp", factory, new Vector3(-4.95f, 2.5f, -0.59f), new Vector3(0.15f, 0.05f, 0.12f), steel);
            Box("Riser clamp", factory, new Vector3(-4.95f, 3.35f, -0.59f), new Vector3(0.15f, 0.05f, 0.12f), steel);
            Box("Wall light housing", factory, new Vector3(-2.08f, 2.65f, -0.65f), new Vector3(0.12f, 0.63f, 0.15f), dark);
            Box("Wall light diffuser", factory, new Vector3(-2.08f, 2.65f, -0.735f), new Vector3(0.055f, 0.48f, 0.024f), cyan);
            Box("Impact bollard footing", factory, new Vector3(-2.1f, -0.27f, -1.52f), new Vector3(0.39f, 0.08f, 0.39f), steel);
            Box("Impact bollard", factory, new Vector3(-2.1f, 0.13f, -1.52f), new Vector3(0.18f, 0.76f, 0.18f), amber);
            Box("Bollard black band", factory, new Vector3(-2.1f, -0.07f, -1.52f), new Vector3(0.19f, 0.09f, 0.19f), dark);
            Box("Bollard black band", factory, new Vector3(-2.1f, 0.18f, -1.52f), new Vector3(0.19f, 0.09f, 0.19f), dark);
            Box("Bollard black band", factory, new Vector3(-2.1f, 0.43f, -1.52f), new Vector3(0.19f, 0.09f, 0.19f), dark);
            Box("Facade inset shadow", factory, new Vector3(3.44f, 1.75f, -0.331f), new Vector3(2.95f, 3.9f, 0.08f), dark);
            Box("Architectural cladding", factory, new Vector3(2.49f, 1.77f, -0.39f), new Vector3(0.91f, 3.73f, 0.12f), cream);
            Box("Cladding lower plinth", factory, new Vector3(2.49f, -0.05f, -0.49f), new Vector3(0.91f, 0.27f, 0.16f), steel);
            Box("Architectural cladding", factory, new Vector3(3.44f, 1.77f, -0.39f), new Vector3(0.91f, 3.73f, 0.12f), cream);
            Box("Cladding lower plinth", factory, new Vector3(3.44f, -0.05f, -0.49f), new Vector3(0.91f, 0.27f, 0.16f), steel);
            Box("Architectural cladding", factory, new Vector3(4.39f, 1.77f, -0.39f), new Vector3(0.91f, 3.73f, 0.12f), cream);
            Box("Cladding lower plinth", factory, new Vector3(4.39f, -0.05f, -0.49f), new Vector3(0.91f, 0.27f, 0.16f), steel);
            Box("Observation window frame", factory, new Vector3(3.44f, 2.5f, -0.49f), new Vector3(2.54f, 1.13f, 0.2f), steel);
            Box("Observation window recess", factory, new Vector3(3.44f, 2.5f, -0.603f), new Vector3(2.36f, 0.95f, 0.045f), dark);
            Box("Window interior light", factory, new Vector3(2.5775f, 2.69f, -0.635f), new Vector3(0.52f, 0.045f, 0.012f), cyan);
            Box("Window lower reflection", factory, new Vector3(2.5775f, 2.24f, -0.635f), new Vector3(0.52f, 0.016f, 0.012f), cyan);
            Box("Window mullion", factory, new Vector3(2.865f, 2.5f, -0.66f), new Vector3(0.026f, 0.94f, 0.065f), steel);
            Box("Window interior light", factory, new Vector3(3.1525f, 2.69f, -0.635f), new Vector3(0.52f, 0.045f, 0.012f), cyan);
            Box("Window lower reflection", factory, new Vector3(3.1525f, 2.24f, -0.635f), new Vector3(0.52f, 0.016f, 0.012f), cyan);
            Box("Window mullion", factory, new Vector3(3.44f, 2.5f, -0.66f), new Vector3(0.026f, 0.94f, 0.065f), steel);
            Box("Window interior light", factory, new Vector3(3.7275f, 2.69f, -0.635f), new Vector3(0.52f, 0.045f, 0.012f), cyan);
            Box("Window lower reflection", factory, new Vector3(3.7275f, 2.24f, -0.635f), new Vector3(0.52f, 0.016f, 0.012f), cyan);
            Box("Window mullion", factory, new Vector3(4.015f, 2.5f, -0.66f), new Vector3(0.026f, 0.94f, 0.065f), steel);
            Box("Window interior light", factory, new Vector3(4.3025f, 2.69f, -0.635f), new Vector3(0.52f, 0.045f, 0.012f), cyan);
            Box("Window lower reflection", factory, new Vector3(4.3025f, 2.24f, -0.635f), new Vector3(0.52f, 0.016f, 0.012f), cyan);
            Box("Window sill", factory, new Vector3(3.44f, 1.99f, -0.64f), new Vector3(2.66f, 0.075f, 0.38f), steel);
            Box("Vent surround", factory, new Vector3(3.44f, 0.93f, -0.49f), new Vector3(2.08f, 0.9f, 0.18f), steel);
            Box("Vent recess", factory, new Vector3(3.44f, 0.93f, -0.59f), new Vector3(1.91f, 0.73f, 0.04f), dark);
            Box("Vent horizontal louvre", factory, new Vector3(3.44f, 0.65f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(3.44f, 0.74f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(3.44f, 0.83f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(3.44f, 0.92f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(3.44f, 1.01f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(3.44f, 1.1f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent horizontal louvre", factory, new Vector3(3.44f, 1.19f, -0.635f), new Vector3(1.84f, 0.037f, 0.075f), slate);
            Box("Vent captive fastener", factory, new Vector3(2.53f, 0.58f, -0.65f), new Vector3(0.038f, 0.038f, 0.018f), cream);
            Box("Vent captive fastener", factory, new Vector3(2.53f, 1.28f, -0.65f), new Vector3(0.038f, 0.038f, 0.018f), cream);
            Box("Vent captive fastener", factory, new Vector3(4.35f, 0.58f, -0.65f), new Vector3(0.038f, 0.038f, 0.018f), cream);
            Box("Vent captive fastener", factory, new Vector3(4.35f, 1.28f, -0.65f), new Vector3(0.038f, 0.038f, 0.018f), cream);
            Box("Exterior service riser", factory, new Vector3(4.95f, 1.64f, -0.54f), new Vector3(0.065f, 3.65f, 0.075f), steel);
            Box("Riser clamp", factory, new Vector3(4.95f, 0.2f, -0.59f), new Vector3(0.15f, 0.05f, 0.12f), steel);
            Box("Riser clamp", factory, new Vector3(4.95f, 1.35f, -0.59f), new Vector3(0.15f, 0.05f, 0.12f), steel);
            Box("Riser clamp", factory, new Vector3(4.95f, 2.5f, -0.59f), new Vector3(0.15f, 0.05f, 0.12f), steel);
            Box("Riser clamp", factory, new Vector3(4.95f, 3.35f, -0.59f), new Vector3(0.15f, 0.05f, 0.12f), steel);
            Box("Wall light housing", factory, new Vector3(2.08f, 2.65f, -0.65f), new Vector3(0.12f, 0.63f, 0.15f), dark);
            Box("Wall light diffuser", factory, new Vector3(2.08f, 2.65f, -0.735f), new Vector3(0.055f, 0.48f, 0.024f), cyan);
            Box("Impact bollard footing", factory, new Vector3(2.1f, -0.27f, -1.52f), new Vector3(0.39f, 0.08f, 0.39f), steel);
            Box("Impact bollard", factory, new Vector3(2.1f, 0.13f, -1.52f), new Vector3(0.18f, 0.76f, 0.18f), amber);
            Box("Bollard black band", factory, new Vector3(2.1f, -0.07f, -1.52f), new Vector3(0.19f, 0.09f, 0.19f), dark);
            Box("Bollard black band", factory, new Vector3(2.1f, 0.18f, -1.52f), new Vector3(0.19f, 0.09f, 0.19f), dark);
            Box("Bollard black band", factory, new Vector3(2.1f, 0.43f, -1.52f), new Vector3(0.19f, 0.09f, 0.19f), dark);
            Box("Sign mounting bolt", factory, new Vector3(-2.58f, 3.79f, -0.66f), new Vector3(0.045f, 0.045f, 0.025f), cream);
            Box("Sign mounting bolt", factory, new Vector3(-2.58f, 4.33f, -0.66f), new Vector3(0.045f, 0.045f, 0.025f), cream);
            Box("Sign mounting bolt", factory, new Vector3(2.58f, 3.79f, -0.66f), new Vector3(0.045f, 0.045f, 0.025f), cream);
            Box("Sign mounting bolt", factory, new Vector3(2.58f, 4.33f, -0.66f), new Vector3(0.045f, 0.045f, 0.025f), cream);
            Box("Access panel enclosure", factory, new Vector3(2.08f, 1.3f, -0.68f), new Vector3(0.25f, 0.46f, 0.22f), steel);
            Box("Access panel face", factory, new Vector3(2.08f, 1.3f, -0.8f), new Vector3(0.2f, 0.38f, 0.028f), dark);
            Box("Access status light", factory, new Vector3(2.08f, 1.42f, -0.82f), new Vector3(0.12f, 0.024f, 0.012f), cyan);
            Box("Access reader", factory, new Vector3(2.08f, 1.25f, -0.82f), new Vector3(0.1f, 0.12f, 0.018f), slate);
        }

        private static void BuildOpeningDoorDetails(Transform door, int side)
        {
            Material cyan = FactoryMaterial("ArchiveCyan");
            Material dark = FactoryMaterial("ArchiveCharcoal");
            Material steel = FactoryMaterial("AgedSteel");
            Box("Door inspection frame", door, new Vector3(0f, 0.55f, -0.19f), new Vector3(1.09f, 0.61f, 0.1f), steel);
            Box("Door inspection glass", door, new Vector3(0f, 0.55f, -0.248f), new Vector3(0.91f, 0.43f, 0.022f), dark);
            Box("Door inspection highlight", door, new Vector3(0f, 0.67f, -0.263f), new Vector3(0.81f, 0.026f, 0.01f), cyan);
            Box("Door handle pocket", door, new Vector3(-side * .48f, -.27f, -0.21f), new Vector3(0.17f, 0.47f, 0.06f), dark);
            Box("Door pull handle", door, new Vector3(-side * .48f, -.27f, -0.27f), new Vector3(0.045f, 0.31f, 0.07f), steel);
            Box("Door reinforced rail", door, new Vector3(0f, -1.23f, -0.19f), new Vector3(1.5f, 0.1f, 0.08f), steel);
            Box("Door reinforced rail", door, new Vector3(0f, 1.31f, -0.19f), new Vector3(1.5f, 0.1f, 0.08f), steel);
        }

        private static Material FactoryMaterial(string name)
            => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/CNNArchiveOpening/Entrance{name}.mat");

        private static Button CreateCinematicButton(Canvas canvas, string name, string label, float width, int fontSize = 48)
        {
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            Image face = ImageAt(canvas.transform, name, new Vector2(width, 122), new Color(.1f, .38f, .37f, 1));
            face.raycastTarget = true;
            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            TextAt(face.transform, "Button Label", label, Vector2.zero, new Vector2(width - 20, 110), fontSize);
            return button;
        }

        private static void BuildEraProp(Transform root,int era)
        {
            // Portrait archive plates keep their aspect ratio and surround the viewer at ±60°.
            for(int side=-1;side<=1;side+=2)
            {
                int index=(era+(side<0?7:1))%8;
                CinematicText.ArchiveClip clip=_content.EndingSchedule.Montage.Single(entry => entry.PropIndex == index).Clip;
                string path="Assets/Materials/OpeningEnding/ArchiveMontage"+index+".mat";
                Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(Shader.Find("CNN/ArchivePhoto"));AssetDatabase.CreateAsset(m,path);}
                m.mainTexture=clip.Still;m.mainTextureScale=clip.UV.size;m.mainTextureOffset=clip.UV.position;m.SetFloat("_Brightness",.3f);
                Transform plate=Primitive("Peripheral memory",root,new Vector3(side*3.3f,1.9f,.3f),new Vector3(2.4f,3.2f,1),m,PrimitiveType.Quad);
                plate.localRotation=Quaternion.Euler(0,side*-62,0);
            }
            if(era==0) {Envelope(root,"Cheque",new Vector3(0,1.3f,0));}
            else if(era==1)
            {
                for(int i=0;i<24;i++)
                {
                    Transform card=Box("Photo paper",root,new Vector3((i%6-2.5f)*.28f,.6f+(i/6)*.2f,.5f),new Vector3(.24f,.16f,.005f),_paper);
                    Material photo=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/OpeningEnding/ArchiveMontage"+(i%8)+".mat");
                    if(photo!=null){Primitive("Photograph",root,card.localPosition+new Vector3(0,0,-.006f),new Vector3(.22f,.14f,1),photo,PrimitiveType.Quad);}
                }
            }
            else if(era==2||era==7)
            {Box("Phone",root,new Vector3(.5f,1.3f,0),new Vector3(.18f,.33f,.025f),_dark);Recognition(root,"Phone lock",new Vector3(.5f,1.3f,-.02f),new Vector2(.13f,.22f),_cyan,"");}
            else if(era==3)
            {Box("Barrier pillar",root,new Vector3(1.5f,.6f,.8f),new Vector3(.3f,1.2f,.3f),_paper);Transform arm=Box("Barrier arm",root,new Vector3(.5f,1.4f,.8f),new Vector3(2.1f,.07f,.08f),_amber);arm.localRotation=Quaternion.Euler(0,0,-25);}
            else if(era==4)
            {Primitive("Robot vacuum",root,new Vector3(-.6f,.09f,-.3f),new Vector3(.35f,.06f,.35f),_dark,PrimitiveType.Cylinder);}
            else if(era==5)
            {Box("Road line",root,new Vector3(-1,.01f,0),new Vector3(.1f,.01f,4),_paper);Box("Road line",root,new Vector3(1,.01f,0),new Vector3(.1f,.01f,4),_paper);}
            else {Recognition(root,"Radiology frame",new Vector3(0,1.7f,0),new Vector2(.8f,1.1f),_cyan,"");}
        }

        private static void BuildAudio(Transform stage,CinematicAudioController audio)
        {
            Assign(audio,"_screen",Source(stage,"Screen muffled",new Vector3(0,1.8f,3),true,.12f,3000));
            Assign(audio,"_voice",Source(stage,"Narration",new Vector3(0,1.7f,2),false,.75f));
            Assign(audio,"_type",Source(stage,"Typewriter",new Vector3(0,1.3f,2.5f),false,1));
            Assign(audio,"_impact",Source(stage,"Score hit",new Vector3(0,1.7f,2),false,1));
            Assign(audio,"_rewind",Source(stage,"Tape rewind",new Vector3(0,1.7f,3),false,.3f));
            Assign(audio,"_pad",Source(stage,"Optional score",Vector3.zero,true,.15f));
            Assign(audio,"_stamp",Source(stage,"Stamp",new Vector3(-1.8f,1.18f,3.3f),false,1));
            Assign(audio,"_beep",Source(stage,"Recognition click",new Vector3(0,1.5f,3),false,1));
        }
        private static AudioSource Source(Transform parent,string name,Vector3 position,bool loop,float volume,float cutoff=22000)
        {
            Transform root=Root(name,parent);root.localPosition=position;
            AudioSource source=root.gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;source.volume=volume;
            source.spatialBlend=1;source.minDistance=2;source.maxDistance=18;source.rolloffMode=AudioRolloffMode.Logarithmic;source.dopplerLevel=0;
            root.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=cutoff;
            return source;
        }
        private static Transform Envelope(Transform parent,string name,Vector3 position)
        {
            Transform root=Root(name,parent);root.localPosition=position;
            Box("Cream envelope",root,Vector3.zero,new Vector3(.4f,.23f,.006f),_paper);
            for(int i=0;i<2;i++){Transform flap=Box("Fold",root,new Vector3(i==0?-.09f:.09f,.025f,-.005f),new Vector3(.23f,.003f,.002f),_wood);flap.localRotation=Quaternion.Euler(0,0,i==0?-26:26);}
            Box("Postage",root,new Vector3(.14f,.07f,-.006f),new Vector3(.045f,.05f,.001f),_metal);
            Canvas c=CanvasAt(root,"Postcode",new Vector3(0,-.045f,-.009f),new Vector2(300,80),.0007f);
            Text text=TextAt(c.transform,"Handwritten code","12345",Vector2.zero,new Vector2(300,80),36);text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumPenScript-Regular.ttf");text.color=new Color(.13f,.17f,.2f);
            return root;
        }
        private static Transform Recognition(Transform parent,string name,Vector3 position,Vector2 size,Material material,string label,int fontSize = 25)
        {
            Transform root=Root(name,parent);root.localPosition=position;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
            {
                Box("Corner h",root,new Vector3(x*(size.x*.5f-.045f),y*size.y*.5f,0),new Vector3(.09f,.004f,.004f),material);
                Box("Corner v",root,new Vector3(x*size.x*.5f,y*(size.y*.5f-.035f),0),new Vector3(.004f,.07f,.004f),material);
            }
            if(label.Length>0)
            {Canvas c=CanvasAt(root,"Label",new Vector3(-size.x*.2f,size.y*.5f+.04f,0),new Vector2(250,65),.0007f);TextAt(c.transform,"Code",label,Vector2.zero,new Vector2(250,65),fontSize);}
            return root;
        }
        private static Material Material(string name,Color color,bool unlit=false)
        {
            string path="Assets/Materials/OpeningEnding/"+name+".mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.enableInstancing=true;if(material.HasProperty("_Smoothness")){material.SetFloat("_Smoothness",.18f);}
            bool oak = name == "ArchiveOak";
            if (oak || name == "ArchivePaper")
            {
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/OpeningEnding/ArchiveMaterials.png"));
                material.SetTextureScale("_BaseMap",new Vector2(.495f,.495f));
                material.SetTextureOffset("_BaseMap", oak ? new Vector2(.0025f, .5025f) : new Vector2(.5025f, .0025f));
                material.color = oak ? new Color(.72f, .65f, .53f) : new Color(.73f, .72f, .66f);
            }
            return material;
        }
        private static Transform Root(string name,Transform parent){Transform t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        private static Transform Box(string name,Transform parent,Vector3 position,Vector3 size,Material material)=>Primitive(name,parent,position,size,material,PrimitiveType.Cube);
        private static Transform Primitive(string name,Transform parent,Vector3 position,Vector3 size,Material material,PrimitiveType primitive)
        {
            GameObject obj=GameObject.CreatePrimitive(primitive);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;
            obj.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return obj.transform;
        }
        private static Canvas CanvasAt(Transform parent,string name,Vector3 position,Vector2 size,float scale)
        {
            GameObject obj=new GameObject(name,typeof(RectTransform),typeof(Canvas));obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=Vector3.one*scale;
            Canvas canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;((RectTransform)obj.transform).sizeDelta=size;return canvas;
        }
        private static Text TextAt(Transform parent,string name,string text,Vector2 position,Vector2 size,int fontSize)
        {
            GameObject obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));obj.transform.SetParent(parent,false);
            Text label=obj.GetComponent<Text>();label.font=_font;label.text=text;label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.96f,.96f,.94f);label.raycastTarget=false;label.supportRichText=true;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;label.rectTransform.sizeDelta=size;label.rectTransform.anchoredPosition=position;return label;
        }
        private static Image ImageAt(Transform parent,string name,Vector2 size,Color color)
        {
            GameObject obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));obj.transform.SetParent(parent,false);Image image=obj.GetComponent<Image>();image.rectTransform.sizeDelta=size;image.color=color;image.raycastTarget=false;return image;
        }
        private static void Assign(UnityEngine.Object obj,string property,UnityEngine.Object value){SerializedObject s=new SerializedObject(obj);s.FindProperty(property).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Array<T>(UnityEngine.Object obj,string property,List<T> values) where T:UnityEngine.Object
        {SerializedObject s=new SerializedObject(obj);SerializedProperty a=s.FindProperty(property);a.arraySize=values.Count;for(int i=0;i<values.Count;i++){a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Value(UnityEngine.Object obj,string property,bool value){SerializedObject s=new SerializedObject(obj);s.FindProperty(property).boolValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Value(UnityEngine.Object obj,string property,string value){SerializedObject s=new SerializedObject(obj);s.FindProperty(property).stringValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    }
}
