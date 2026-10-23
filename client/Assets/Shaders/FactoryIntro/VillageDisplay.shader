// Original procedural broadcasts for this project. No footage, logos, font textures or external images.
Shader "FactoryIntro/VillageDisplay"
{
    Properties
    {
        _PlaybackTime("Programme playback time", Float) = 0
        _SignalTime("Signal time", Float) = 0
        _Damage("Lost image quality", Range(0, 1)) = 0
        _Interference("Signal interference", Range(0, 1)) = 0
        _Phase("Broadcast offset", Range(0, 1)) = 0
        [Enum(News,0,Football,1,Film,2)] _Channel("Original programme", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "VillageBroadcast"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PlaybackTime;
                float _SignalTime;
                float _Damage;
                float _Interference;
                float _Phase;
                float _Channel;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 seed)
            {
                return frac(sin(dot(seed, float2(127.1, 311.7))) * 43758.5453);
            }

            float Box(float2 uv, float2 center, float2 halfSize)
            {
                float2 edge = abs(uv - center) - halfSize;
                return 1.0 - smoothstep(-0.001, 0.001, max(edge.x, edge.y));
            }

            float BoxOutline(float2 uv, float2 center, float2 halfSize, float width)
            {
                return saturate(Box(uv, center, halfSize) - Box(uv, center, halfSize - width));
            }

            float Ellipse(float2 uv, float2 center, float2 radius)
            {
                return 1.0 - smoothstep(0.97, 1.03, length((uv - center) / radius));
            }

            float Disc(float2 uv, float2 center, float radius)
            {
                return Ellipse(uv, center, float2(radius * 0.5625, radius));
            }

            float Segment(float2 uv, float2 from, float2 to, float width)
            {
                float2 relative = (uv - from) * float2(1.77778, 1.0);
                float2 direction = (to - from) * float2(1.77778, 1.0);
                float fraction = saturate(dot(relative, direction) / max(dot(direction, direction), 0.00001));
                return 1.0 - smoothstep(width, width + 0.002, length(relative - direction * fraction));
            }

            float Triangle(float2 uv, float2 a, float2 b, float2 c)
            {
                float ab = (uv.x - b.x) * (a.y - b.y) - (a.x - b.x) * (uv.y - b.y);
                float bc = (uv.x - c.x) * (b.y - c.y) - (b.x - c.x) * (uv.y - c.y);
                float ca = (uv.x - a.x) * (c.y - a.y) - (c.x - a.x) * (uv.y - a.y);
                return saturate(step(0.0, min(ab, min(bc, ca))) + step(max(ab, max(bc, ca)), 0.0));
            }

            // Hand-authored 3 x 5 block letters: fifteen bits fit exactly in a float.
            float GlyphBits(int code)
            {
                switch (code)
                {
                    case 45: return 448.0;
                    case 46: return 2.0;
                    case 58: return 1040.0;
                    case 48: return 31599.0;
                    case 49: return 9879.0;
                    case 50: return 14479.0;
                    case 51: return 14499.0;
                    case 52: return 23524.0;
                    case 53: return 29411.0;
                    case 54: return 25583.0;
                    case 55: return 30866.0;
                    case 56: return 31727.0;
                    case 57: return 31715.0;
                    case 65: return 11245.0;
                    case 66: return 15083.0;
                    case 67: return 25166.0;
                    case 68: return 15211.0;
                    case 69: return 29391.0;
                    case 70: return 29385.0;
                    case 71: return 25454.0;
                    case 72: return 23533.0;
                    case 73: return 29847.0;
                    case 74: return 18730.0;
                    case 75: return 23277.0;
                    case 76: return 4687.0;
                    case 77: return 24557.0;
                    case 78: return 24573.0;
                    case 79: return 11114.0;
                    case 80: return 15049.0;
                    case 81: return 11134.0;
                    case 82: return 15085.0;
                    case 83: return 25251.0;
                    case 84: return 29842.0;
                    case 85: return 23407.0;
                    case 86: return 23402.0;
                    case 87: return 23549.0;
                    case 88: return 23213.0;
                    case 89: return 23186.0;
                    case 90: return 30863.0;
                    default: return 0.0;
                }
            }

            float Text4(float2 uv, float2 origin, float height, int4 letters)
            {
                float2 local = (uv - origin) / float2(height * 0.45, height);
                int cell = (int)floor(local.x);
                if (cell < 0 || cell > 3 || local.y < 0.0 || local.y >= 1.0)
                {
                    return 0.0;
                }
                float column = floor(frac(local.x) * 4.0);
                if (column > 2.0)
                {
                    return 0.0;
                }
                float bit = floor(local.y * 5.0) * 3.0 + column;
                return fmod(floor(GlyphBits(letters[cell]) / exp2(bit)), 2.0);
            }

            float Text8(float2 uv, float2 origin, float height, int4 first, int4 second)
            {
                return Text4(uv, origin, height, first) +
                    Text4(uv, origin + float2(height * 1.8, 0.0), height, second);
            }

            float Number2(float2 uv, float2 origin, float height, float value)
            {
                int number = (int)floor(fmod(max(0.0, value), 100.0));
                return Text4(uv, origin, height, int4(48 + number / 10, 48 + number % 10, 32, 32));
            }

            half3 News(float2 uv, float time)
            {
                half3 color = lerp(half3(0.015, 0.045, 0.11), half3(0.07, 0.25, 0.38), uv.y);
                float studioLines = step(0.95, frac(uv.x * 16.0)) * Box(uv, float2(0.5, 0.59), float2(0.5, 0.28));
                color += studioLines * half3(0.03, 0.11, 0.14);
                color = lerp(color, half3(0.08, 0.54, 0.68), Box(uv, float2(0.5, 0.36), float2(0.5, 0.006)));

                // The inset globe and changing report bars establish a live studio programme.
                float inset = Box(uv, float2(0.77, 0.61), float2(0.195, 0.235));
                color = lerp(color, half3(0.025, 0.12, 0.2), inset);
                color = lerp(color, half3(0.12, 0.68, 0.77), BoxOutline(uv, float2(0.77, 0.61), float2(0.195, 0.235), 0.004));
                float2 globe = (uv - float2(0.77, 0.65)) / float2(0.09, 0.16);
                float globeMask = 1.0 - smoothstep(0.98, 1.02, length(globe));
                float2 map = float2(frac(globe.x * 0.38 + time * 0.045), globe.y * 0.5 + 0.5);
                float continents = saturate(Ellipse(map, float2(0.23, 0.62), float2(0.12, 0.22)) +
                    Ellipse(map, float2(0.35, 0.34), float2(0.065, 0.19)) + Ellipse(map, float2(0.72, 0.55), float2(0.17, 0.13)));
                half3 globeColor = lerp(half3(0.025, 0.31, 0.61), half3(0.15, 0.83, 0.65), continents);
                globeColor *= 0.65 + 0.35 * saturate(1.0 - length(globe) * 0.65 - globe.x * 0.3);
                float grid = step(0.96, frac(map.x * 9.0)) + step(0.97, frac(map.y * 7.0));
                color = lerp(color, globeColor + grid * 0.07, globeMask);
                [unroll]
                for (int bar = 0; bar < 5; bar++)
                {
                    float amount = 0.04 + 0.035 * (0.5 + 0.5 * sin(time * 1.2 + bar * 1.4));
                    color = lerp(color, half3(0.08, 0.75, 0.8),
                        Box(uv, float2(0.64 + bar * 0.064, 0.425 + amount * 0.5), float2(0.021, amount * 0.5)));
                }

                float sway = sin(time * 0.9) * 0.009;
                float nod = sin(time * 1.7) * 0.009;
                float2 head = float2(0.325 + sway, 0.685 + nod);
                color = lerp(color, half3(0.12, 0.18, 0.3), Ellipse(uv, float2(0.325, 0.425), float2(0.135, 0.21)));
                color = lerp(color, half3(0.94, 0.96, 0.93), Triangle(uv, float2(0.29, 0.6), float2(0.36, 0.6), float2(0.325, 0.36)));
                color = lerp(color, half3(0.13, 0.75, 0.82), Triangle(uv, float2(0.315, 0.565), float2(0.335, 0.565), float2(0.325, 0.41)));
                color = lerp(color, half3(0.72, 0.43, 0.27), Box(uv, float2(head.x, 0.59), float2(0.023, 0.045)));
                color = lerp(color, half3(0.87, 0.63, 0.43), Ellipse(uv, head, float2(0.054, 0.102)));
                float hair = Ellipse(uv, head + float2(-0.005, 0.058), float2(0.058, 0.07));
                hair *= smoothstep(head.y + 0.015, head.y + 0.045, uv.y);
                color = lerp(color, half3(0.08, 0.045, 0.035), hair);
                float blink = step(0.13, fmod(time, 4.2));
                color = lerp(color, half3(0.045, 0.035, 0.03),
                    Ellipse(uv, head + float2(-0.02, 0.012), float2(0.0045, 0.002 + blink * 0.006)) +
                    Ellipse(uv, head + float2(0.02, 0.012), float2(0.0045, 0.002 + blink * 0.006)));
                float speech = 0.004 + abs(sin(time * 7.0)) * 0.01;
                color = lerp(color, half3(0.31, 0.09, 0.065), Ellipse(uv, head + float2(0.0, -0.043), float2(0.015, speech)));
                float2 hand = float2(0.46 + sin(time * 1.3) * 0.025, 0.48 + sin(time * 1.8) * 0.045);
                color = lerp(color, half3(0.12, 0.18, 0.3), Segment(uv, float2(0.42, 0.42), hand, 0.027));
                color = lerp(color, half3(0.87, 0.63, 0.43), Disc(uv, hand, 0.025));
                color = lerp(color, half3(0.055, 0.11, 0.17), Box(uv, float2(0.33, 0.325), float2(0.3, 0.055)));
                color = lerp(color, half3(0.66, 0.85, 0.88), Box(uv, float2(0.33, 0.38), float2(0.3, 0.008)));
                color = lerp(color, half3(0.87, 0.9, 0.9), Box(uv, float2(0.29, 0.391), float2(0.067, 0.006)));

                color = lerp(color, half3(0.025, 0.46, 0.6), Box(uv, float2(0.5, 0.205), float2(0.5, 0.065)));
                float headline = Text8(uv, float2(0.055, 0.184), 0.052, int4(86,73,76,76), int4(65,71,69,32));
                headline += Text8(uv, float2(0.25, 0.184), 0.052, int4(85,80,68,65), int4(84,69,32,32));
                color = lerp(color, half3(0.96, 0.99, 1.0), saturate(headline));
                color = lerp(color, half3(0.02, 0.06, 0.1), Box(uv, float2(0.5, 0.078), float2(0.5, 0.05)));
                float2 ticker = float2(frac(uv.x + time * 0.065), uv.y);
                float tickerText = Text8(ticker, float2(0.02, 0.058), 0.041, int4(67,76,69,65), int4(82,32,32,32));
                tickerText += Text8(ticker, float2(0.18, 0.058), 0.041, int4(83,75,73,69), int4(83,32,32,32));
                tickerText += Text8(ticker, float2(0.34, 0.058), 0.041, int4(84,79,68,65), int4(89,32,32,32));
                tickerText += Text8(ticker, float2(0.55, 0.058), 0.041, int4(86,73,76,76), int4(65,71,69,32));
                tickerText += Text4(ticker, float2(0.72, 0.058), 0.041, int4(78,69,87,83));
                color = lerp(color, half3(0.83, 0.91, 0.94), saturate(tickerText));
                color = lerp(color, half3(0.95, 0.98, 1.0), Text4(uv, float2(0.055, 0.88), 0.065, int4(78,69,87,83)));
                color = lerp(color, half3(0.78, 0.055, 0.075), Box(uv, float2(0.9, 0.915), float2(0.075, 0.042)));
                color = lerp(color, half3(1.0, 0.96, 0.94), Text4(uv, float2(0.846, 0.893), 0.045, int4(76,73,86,69)));
                return color;
            }

            float2 FootballBall(float cycle)
            {
                if (cycle < 2.0) return lerp(float2(0.21, 0.35), float2(0.43, 0.61), cycle * 0.5);
                if (cycle < 4.0) return lerp(float2(0.43, 0.61), float2(0.66, 0.36), (cycle - 2.0) * 0.5);
                if (cycle < 5.3) return lerp(float2(0.66, 0.36), float2(0.8, 0.52), (cycle - 4.0) / 1.3);
                if (cycle < 6.2) return lerp(float2(0.8, 0.52), float2(0.958, 0.48), (cycle - 5.3) / 0.9);
                return float2(0.958, 0.48);
            }

            half3 Football(float2 uv, float time)
            {
                float cycle = fmod(time, 8.0);
                half3 color = half3(0.04, 0.065, 0.09);
                float crowd = Hash(floor(uv * float2(95.0, 50.0)));
                color += half3(0.16, 0.18, 0.23) * step(0.6, crowd) * (0.8 + 0.2 * sin(time * 2.5 + uv.x * 24.0));
                float pitch = Box(uv, float2(0.5, 0.49), float2(0.45, 0.35));
                half3 turf = lerp(half3(0.055, 0.37, 0.15), half3(0.07, 0.46, 0.19), step(0.5, frac(uv.x * 12.0)));
                color = lerp(color, turf, pitch);
                float markings = BoxOutline(uv, float2(0.5, 0.49), float2(0.44, 0.34), 0.003);
                markings += Box(uv, float2(0.5, 0.49), float2(0.0018, 0.34));
                markings += saturate(Disc(uv, float2(0.5, 0.49), 0.11) - Disc(uv, float2(0.5, 0.49), 0.105));
                markings += BoxOutline(uv, float2(0.12, 0.49), float2(0.06, 0.18), 0.003);
                markings += BoxOutline(uv, float2(0.88, 0.49), float2(0.06, 0.18), 0.003);
                markings += Disc(uv, float2(0.5, 0.49), 0.006);
                color = lerp(color, half3(0.85, 0.92, 0.8), saturate(markings));
                float nets = BoxOutline(uv, float2(0.955, 0.49), float2(0.018, 0.09), 0.003) +
                    BoxOutline(uv, float2(0.045, 0.49), float2(0.018, 0.09), 0.003);
                float netArea = Box(uv, float2(0.955, 0.49), float2(0.018, 0.09)) + Box(uv, float2(0.045, 0.49), float2(0.018, 0.09));
                nets += netArea * saturate(step(0.9, frac(uv.x * 160.0)) + step(0.9, frac(uv.y * 65.0))) * 0.6;
                color = lerp(color, half3(0.88, 0.93, 0.94), saturate(nets));

                float2 ball = FootballBall(cycle);
                [unroll]
                for (int player = 0; player < 10; player++)
                {
                    float team = player < 5 ? 0.0 : 1.0;
                    float member = player < 5 ? player : player - 5;
                    float2 home = member < 0.5 ? float2(0.21, 0.35) :
                        member < 1.5 ? float2(0.43, 0.61) :
                        member < 2.5 ? float2(0.66, 0.36) : float2(0.8, 0.52);
                    home += float2(team * 0.055, team * (0.98 - home.y * 2.0));
                    if (member > 3.5)
                    {
                        home = float2(lerp(0.09, 0.915, team), 0.49);
                    }
                    float2 run = float2(sin(time * 1.1 + player * 1.6) * 0.035, cos(time * 1.4 + player * 1.2) * 0.03);
                    float2 position = lerp(home + run, ball, team < 0.5 ? 0.16 : 0.09);
                    if (abs(uv.x - position.x) < 0.03 && abs(uv.y - position.y) < 0.065)
                    {
                        float stride = sin(time * 13.0 + player) * 0.015;
                        color = lerp(color, half3(0.025, 0.18, 0.07), Ellipse(uv, position + float2(0.007, -0.025), float2(0.019, 0.012)));
                        color = lerp(color, half3(0.035, 0.055, 0.08),
                            Segment(uv, position + float2(-0.005, -0.008), position + float2(-0.009, -0.035 + stride), 0.005) +
                            Segment(uv, position + float2(0.005, -0.008), position + float2(0.009, -0.035 - stride), 0.005));
                        half3 shirt = team < 0.5 ? half3(0.93, 0.27, 0.075) : half3(0.07, 0.52, 0.95);
                        color = lerp(color, shirt, Ellipse(uv, position, float2(0.015, 0.022)));
                        color = lerp(color, half3(0.88, 0.65, 0.4), Disc(uv, position + float2(0.0, 0.026), 0.013));
                        color = lerp(color, half3(0.97, 0.98, 0.95), Box(uv, position, float2(0.002, 0.009)));
                    }
                }
                float kickArc = cycle < 6.2 ? abs(sin(cycle * 3.14159 / 2.0)) * 0.025 : 0.0;
                color = lerp(color, half3(0.025, 0.15, 0.05), Ellipse(uv, ball + float2(0.006, -0.012), float2(0.01, 0.006)));
                ball.y += kickArc;
                color = lerp(color, half3(1.0, 0.99, 0.92), Disc(uv, ball, 0.016));
                color = lerp(color, half3(0.045, 0.055, 0.07), Disc(uv, ball + float2(sin(time * 12.0) * 0.003, 0.0), 0.006));

                color = lerp(color, half3(0.015, 0.045, 0.08), Box(uv, float2(0.5, 0.935), float2(0.5, 0.065)));
                color = lerp(color, half3(0.94, 0.96, 1.0), Text4(uv, float2(0.035, 0.915), 0.045, int4(72,79,77,69)));
                color = lerp(color, half3(1.0, 0.5, 0.17), Number2(uv, float2(0.15, 0.903), 0.064, floor(time / 8.0) + step(6.2, cycle)));
                color = lerp(color, half3(0.94, 0.96, 1.0), Text4(uv, float2(0.232, 0.91), 0.05, int4(45,32,32,32)));
                color = lerp(color, half3(0.2, 0.7, 1.0), Number2(uv, float2(0.275, 0.903), 0.064, 0.0));
                color = lerp(color, half3(0.94, 0.96, 1.0), Text4(uv, float2(0.36, 0.915), 0.045, int4(65,87,65,89)));
                float clock = Number2(uv, float2(0.68, 0.91), 0.055, 12.0 + floor(time / 60.0)) +
                    Text4(uv, float2(0.73, 0.91), 0.055, int4(58,32,32,32)) +
                    Number2(uv, float2(0.755, 0.91), 0.055, fmod(time, 60.0));
                color = lerp(color, half3(0.94, 0.96, 1.0), saturate(clock));
                color = lerp(color, half3(0.94, 0.96, 1.0), Text4(uv, float2(0.876, 0.915), 0.045, int4(76,73,86,69)));
                float goal = step(6.2, cycle);
                color = lerp(color, half3(0.98, 0.66, 0.1), Box(uv, float2(0.5, 0.08), float2(0.16, 0.045)) * goal);
                color = lerp(color, half3(0.045, 0.07, 0.08), Text4(uv, float2(0.447, 0.056), 0.055, int4(71,79,65,76)) * goal);
                return color;
            }

            half3 Film(float2 uv, float time)
            {
                float2 camera = uv + float2(time * 0.016, sin(time * 0.17) * 0.025);
                half3 color = lerp(half3(0.012, 0.018, 0.055), half3(0.11, 0.045, 0.17), uv.y);
                float nebula = 0.5 + 0.5 * sin(camera.x * 5.0 + sin(camera.y * 7.0));
                color += half3(0.06, 0.075, 0.15) * nebula * nebula;
                float2 starGrid = camera * float2(65.0, 36.0);
                float starSeed = Hash(floor(starGrid));
                float star = (1.0 - smoothstep(0.035, 0.12, length(frac(starGrid) - 0.5))) * step(0.91, starSeed);
                color += star * (0.6 + 0.4 * sin(time * 1.7 + starSeed * 40.0));

                float2 planetCenter = float2(0.79 - sin(time * 0.15) * 0.035, 0.64 + cos(time * 0.13) * 0.025);
                float2 planet = (uv - planetCenter) / float2(0.15, 0.265);
                float ring = saturate(Ellipse(uv, planetCenter, float2(0.23, 0.06)) - Ellipse(uv, planetCenter, float2(0.19, 0.043)));
                color = lerp(color, half3(0.47, 0.55, 0.79), ring * 0.65);
                float planetMask = 1.0 - smoothstep(0.98, 1.02, length(planet));
                float bands = 0.5 + 0.5 * sin(planet.y * 19.0 + sin(planet.x * 3.0 + time * 0.1));
                half3 planetColor = lerp(half3(0.14, 0.33, 0.55), half3(0.35, 0.68, 0.78), bands * 0.6);
                planetColor *= 0.2 + 0.8 * saturate(0.7 - planet.x * 0.65 + planet.y * 0.1);
                color = lerp(color, planetColor, planetMask);

                // An original survey craft crosses the frame; engine thrust and banking visibly animate.
                float2 shipCenter = float2(0.4 + sin(time * 0.43) * 0.16, 0.46 + sin(time * 0.67) * 0.12);
                float2 ship = uv - shipCenter;
                float bank = sin(time * 0.67) * 0.18;
                ship = float2(ship.x * cos(bank) - ship.y * sin(bank), ship.x * sin(bank) + ship.y * cos(bank));
                float flameLength = 0.1 + 0.025 * sin(time * 19.0);
                float exhaust = Triangle(ship, float2(-0.075, -0.022), float2(-0.075, 0.022), float2(-0.075 - flameLength, 0.0));
                color = lerp(color, half3(0.1, 0.57, 1.0), exhaust * 0.85);
                color = lerp(color, half3(0.62, 0.91, 1.0), Triangle(ship, float2(-0.075, -0.01), float2(-0.075, 0.01), float2(-0.12, 0.0)));
                float wing = Triangle(ship, float2(0.11, 0.0), float2(-0.085, 0.1), float2(-0.055, -0.085));
                color = lerp(color, half3(0.64, 0.74, 0.84), wing);
                color = lerp(color, half3(0.25, 0.37, 0.52), Triangle(ship, float2(0.11, 0.0), float2(-0.055, 0.0), float2(-0.055, -0.085)));
                color = lerp(color, half3(0.9, 0.94, 0.98), Ellipse(ship, float2(0.005, 0.009), float2(0.094, 0.024)));
                color = lerp(color, half3(0.03, 0.27, 0.44), Ellipse(ship, float2(0.05, 0.019), float2(0.027, 0.013)));
                color = lerp(color, half3(0.2, 0.94, 1.0), Segment(ship, float2(-0.055, 0.07), float2(-0.025, 0.045), 0.003));
                color = lerp(color, half3(1.0, 0.51, 0.15), Disc(ship, float2(-0.055, -0.06), 0.009) * (0.6 + 0.4 * sin(time * 4.0)));
                float streak = frac(time * 0.12);
                float2 comet = float2(1.12 - streak * 1.4, 0.85 - streak * 0.3);
                color += half3(0.12, 0.2, 0.3) * Segment(uv, comet, comet + float2(0.13, 0.028), 0.0015);

                color *= step(0.12, uv.y) * step(uv.y, 0.88);
                color = lerp(color, half3(0.7, 0.81, 0.93), Text4(uv, float2(0.04, 0.924), 0.037, int4(70,73,76,77)));
                float title = Text8(uv, float2(0.42, 0.046), 0.045, int4(79,82,66,73), int4(84,32,32,32));
                color = lerp(color, half3(0.7, 0.81, 0.93), title);
                return color;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float damage = saturate(_Damage);
                float2 uv = input.uv;
                float playback = _PlaybackTime + _Phase * 9.0;
                float signalFrame = floor(_SignalTime * 11.0);
                float rowNoise = Hash(float2(floor(uv.y * 19.0) + _Phase * 17.0, signalFrame));
                float tear = step(0.74, rowNoise) * _Interference;
                uv.x += (rowNoise - 0.5) * tear * 0.22;

                // Re-evaluate the same programme frame on a collapsing grid. No extra cameras or textures.
                float2 grid = lerp(float2(640.0, 360.0), float2(18.0, 10.0), sqrt(damage));
                float2 blockUV = (floor(uv * grid) + 0.5) / grid;
                uv = saturate(lerp(uv, blockUV, damage));
                half3 picture;
                if (_Channel < 0.5)
                {
                    picture = News(uv, playback);
                }
                else if (_Channel < 1.5)
                {
                    picture = Football(uv, playback);
                }
                else
                {
                    picture = Film(uv, playback);
                }
                float luminance = dot(picture, half3(0.2126, 0.7152, 0.0722));
                picture = lerp(picture, luminance.xxx, damage * 0.45);
                picture = lerp(picture, floor(picture * 6.0 + 0.5) / 6.0, damage * 0.75);
                picture = lerp(picture, half3(0.23, 0.25, 0.29), damage * 0.18);
                picture += (Hash(floor(input.uv * float2(160.0, 90.0)) + signalFrame) - 0.5) * _Interference * 0.35;
                picture += half3(0.2, -0.08, 0.16) * tear;
                picture *= lerp(1.0, 0.78, damage);
                half3 status = lerp(half3(0.16, 0.95, 0.72), half3(0.95, 0.23, 0.12), damage);
                picture = lerp(picture, status, 1.0 - step(0.012, input.uv.y));
                return half4(max(picture, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}
