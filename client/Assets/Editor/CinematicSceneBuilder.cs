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
    public static class CinematicSceneBuilder
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
                Line(.05,3.65,"공장 안에서 그 답을 직접 찾아보세요.", CinematicText.CaptionAnchor.Doors) };
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
            _content.PostOfficeStamp = new CinematicText.ArchiveClip
            {
                Id = "PO_STAMP",
                Still = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/OpeningEnding/PostOfficeStampPlate.png"),
                Video = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Textures/OpeningEnding/PostOfficeStamp.mp4"),
                UV = new Rect(.10f, .035f, .89f, .89f),
                Duration = 2.8f
            };
            _content.PostOfficeEnvelope = new CinematicText.ArchiveClip
            {
                Id = "PO_STAMPED_ENVELOPE",
                Still = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/OpeningEnding/PostOfficeStampedEnvelope.png")
            };
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
            LineRenderer platform=Root("Quiet standing marker",stage).gameObject.AddComponent<LineRenderer>();
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
            Text year=TextAt(yearCanvas.transform,"Year","1989",Vector2.zero,new Vector2(1000,220),100);
            Assign(world,"_year",year);
            Canvas captionCanvas=CanvasAt(stage,"Archive caption",new Vector3(0,.45f,3),new Vector2(1500,100),.002f);
            Text caption=TextAt(captionCanvas.transform,"Provenance",_content.Provenance,Vector2.zero,new Vector2(1500,100),21);
            caption.color=new Color(.75f,.72f,.63f); Assign(world,"_caption",caption);
            Transform timeline=Root("Rewinding history",stage);
            for(int i=0;i<15;i++)
            {
                float x=-i*.85f;
                Box("Timeline rule",timeline,new Vector3(x,.02f,0),new Vector3(.81f,.009f,.015f),_amber);
                Canvas c=CanvasAt(timeline,"Year marker",new Vector3(x,.15f,0),new Vector2(250,90),.002f);
                TextAt(c.transform,"Year",(2026-i*3).ToString(),Vector2.zero,new Vector2(250,90),32);
            }
            Assign(world,"_timeline",timeline);
            List<Transform> boxes=new List<Transform>();
            for(int i=0;i<10;i++)
            {
                Vector3 p=new Vector3((i%5-2)*.72f,1.3f+(i/5)*.65f,3.8f+(i%2)*.35f);
                Transform box=Recognition(stage,"Recognition "+i,p+new Vector3(0,0,-.03f),new Vector2(.45f,.27f),_green,i==0?"12345":(12000+i*173).ToString()); boxes.Add(box);
            }
            Array(world,"_boxes",boxes);
            Transform digit=Root("Handwritten digit",stage); digit.localPosition=new Vector3(0,1.9f,2.4f);
            List<Transform> pixels=new List<Transform>();
            for(int row=0;row<16;row++) for(int col=0;col<16;col++)
            {
                float x=(col-7.5f)/7.5f, y=(row-7.5f)/7.5f;
                bool ink=(row>=2&&row<=4&&col>=4&&col<=11)||(col>=10&&col<=12&&row>=3&&row<=7)
                    ||(row>=6&&row<=11&&Mathf.Abs(col-(17-row))<2)||(row>=11&&row<=13&&col>=3&&col<=12);
                if(!ink) {continue;}
                Vector3 local=new Vector3(x*.8f,-y*.8f,0);
                pixels.Add(Box("Light pixel",stage,local+new Vector3(0,1.9f,2.4f),Vector3.one*.085f,_cyan));
            }
            Canvas handwritten=CanvasAt(digit,"Handwritten numeral",Vector3.zero,new Vector2(330,330),.008f);
            Text numeral=TextAt(handwritten.transform,"Numeral","2",Vector2.zero,new Vector2(330,330),280);
            numeral.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumPenScript-Regular.ttf");
            Assign(world,"_digit",digit);
            Array(world,"_pixels",pixels);
            if (!ending) { Assign(world,"_numeral",numeral); }
            Transform digitBox = Recognition(digit,"Digit recognition",new Vector3(0,0,-.06f),new Vector2(1.7f,1.7f),_green,"2");
            if (!ending) { Assign(world,"_digitBox",digitBox); }
            Transform factory=Root("CNN facade",stage); factory.localPosition=new Vector3(0,0,12);
            Box("Facade",factory,new Vector3(0,2.5f,.7f),new Vector3(12,5,.7f),_dark);
            Box("Door light",factory,new Vector3(0,1.5f,.15f),new Vector3(3.1f,3,.1f),_cyan);
            Transform left=Box("Left door",factory,new Vector3(-.7f,1.5f,0),new Vector3(1.4f,3,.18f),_metal);
            Transform right=Box("Right door",factory,new Vector3(.7f,1.5f,0),new Vector3(1.4f,3,.18f),_metal);
            for(int i=0;i<18;i++)
            { float x=(i%9-4)*1.05f; float y=3.5f+(i/9)*.6f; Box("Circuit",factory,new Vector3(x,y,-.02f),new Vector3(.7f,.018f,.02f),_cyan); }
            Canvas titleCanvas=CanvasAt(factory,"Factory title",new Vector3(0,4.55f,-.1f),new Vector2(1200,220),.004f);
            TextAt(titleCanvas.transform,"Title",_content.FactoryTitle,Vector2.zero,new Vector2(1200,220),90);
            for(int i=0;i<22;i++) {Box("Path light",factory,new Vector3(0,.012f,-i*.5f),new Vector3(.8f,.015f,.045f),_cyan);}
            Assign(world,"_factory",factory);
            if (!ending)
            {
                Assign(world,"_leftDoor",left);
                Assign(world,"_rightDoor",right);
            }
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
            Canvas captions=CanvasAt(stage,"Narration",new Vector3(0,.99f,2.5f),new Vector2(1500,260),.0018f);
            Image backing=ImageAt(captions.transform,"Caption backing",new Vector2(1500,245),new Color(0,0,0,.45f));
            Text text=TextAt(captions.transform,"Type line","",Vector2.zero,new Vector2(1450,240),48);
            Assign(type,"_line",text); Assign(type,"_backing",backing);
            Canvas hints=CanvasAt(stage,"Trigger hold",new Vector3(0,.4f,2.3f),new Vector2(1100,120),.0015f);
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
                Canvas start = CanvasAt(stage, "Opening Start Panel", new Vector3(0, 1.6f, 2.15f), new Vector2(420, 122), .0018f);
                Button startButton = CreateCinematicButton(start, "Start Button", "시작", 420);
                Assign(controls, "_startButton", startButton);
            }
            BuildAudio(stage,audio);
            factory.gameObject.SetActive(false); network.gameObject.SetActive(false);
            foreach(Transform t in boxes.Concat(pixels).Concat(eraProps)) {t.gameObject.SetActive(false);}
            digit.gameObject.SetActive(false); portal.gameObject.SetActive(false); timeline.gameObject.SetActive(false);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/"+name+".unity");
        }

        private static Button CreateCinematicButton(Canvas canvas, string name, string label, float width)
        {
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            Image face = ImageAt(canvas.transform, name, new Vector2(width, 122), new Color(.1f, .38f, .37f, 1));
            face.raycastTarget = true;
            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            TextAt(face.transform, "Button Label", label, Vector2.zero, new Vector2(width - 20, 110), 48);
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
        private static Transform Recognition(Transform parent,string name,Vector3 position,Vector2 size,Material material,string label)
        {
            Transform root=Root(name,parent);root.localPosition=position;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
            {
                Box("Corner h",root,new Vector3(x*(size.x*.5f-.045f),y*size.y*.5f,0),new Vector3(.09f,.004f,.004f),material);
                Box("Corner v",root,new Vector3(x*size.x*.5f,y*(size.y*.5f-.035f),0),new Vector3(.004f,.07f,.004f),material);
            }
            if(label.Length>0)
            {Canvas c=CanvasAt(root,"Label",new Vector3(-size.x*.2f,size.y*.5f+.04f,0),new Vector2(250,65),.0007f);TextAt(c.transform,"Code",label,Vector2.zero,new Vector2(250,65),25);}
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
