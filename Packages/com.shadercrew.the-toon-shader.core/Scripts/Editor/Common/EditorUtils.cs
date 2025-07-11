﻿using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace ShaderCrew.TheToonShader
{
    public static class EditorUtils
    {
        public static void usualStart(string name)
        {
            LogoOnlyStart(name);

            Rect rect = EditorGUILayout.BeginVertical();
            GUI.Box(rect, GUIContent.none);

        }

        public static void LogoOnlyStart(string name)
        {
            Rect screenRect = GUILayoutUtility.GetRect(1, 1);

            Rect vertRect = EditorGUILayout.BeginVertical();
           
            Color backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            EditorGUI.DrawRect(new Rect(screenRect.x - 20, screenRect.y - 5, screenRect.width + 25, vertRect.height + 9), backgroundColor);

            Sprite test = Resources.Load<Sprite>("logo-the-toon-shader-small");

            GUIStyle headStyle = new GUIStyle();
            headStyle.normal.textColor = Color.white;
            headStyle.fontSize = 13;
            headStyle.alignment = TextAnchor.MiddleCenter;
            headStyle.fontStyle = FontStyle.Italic;


            //GUILayout.Label(Strings.THE_TOON_SHADER_TITLE, headStyle);
            //GUILayout.Label(Strings.SHADER_CREW_TITLE, headStyle);
            headStyle.fontStyle = FontStyle.Bold;
            headStyle.fontSize = 14;
            GUILayout.Label(name, headStyle);

            if (test != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                //GUILayout.Label(test.texture, GUILayout.Width(250), GUILayout.Height(150));
                GUILayout.Label(test.texture, GUILayout.Width(300), GUILayout.Height(200));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            //EditorGUILayout.Space(30);

        }

        public static bool LogoOnlyStartWithDescription(string name, string description, bool showDescription)
        {
            LogoOnlyStart(name);

            if (description != null && description != "")
            {
                Rect rectt = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.Box(rectt, GUIContent.none);
                string title = " Description:";

                GUIStyle style = EditorStyles.foldout;
                FontStyle previousStyle = style.fontStyle;
                style.fontStyle = FontStyle.Bold;

                showDescription = EditorGUILayout.Foldout(showDescription, title, style); //, EditorStyles.boldLabel);
                style.fontStyle = previousStyle;


                //showDescription = EditorGUILayout.Foldout(showDescription, title);
                if (showDescription)
                {
                    GUIStyle textStyle = EditorStyles.label;
                    textStyle.wordWrap = true;
                    textStyle.richText = true;
                    textStyle.fontStyle = FontStyle.Normal;
                    GUILayout.Space(10);
                    GUILayout.Label(description, textStyle);
                    GUILayout.Space(10);
                }
                EditorGUILayout.EndVertical();

                EditorUtils.makeHorizontalSeparation();
                return showDescription;

            }
            else
            {
                return false;
            }


        }


        public static bool usualStartWithDescription(string name, string description, bool showDescription)
        {
            usualStart(name);
            if (description != null && description != "")
            {
                string title = " Description:";

                GUIStyle style = EditorStyles.foldout;
                FontStyle previousStyle = style.fontStyle;
                style.fontStyle = FontStyle.Bold;

                showDescription = EditorGUILayout.Foldout(showDescription, title, style); //, EditorStyles.boldLabel);
                style.fontStyle = previousStyle;


                //showDescription = EditorGUILayout.Foldout(showDescription, title);
                if (showDescription)
                {
                    GUIStyle textStyle = EditorStyles.label;
                    textStyle.wordWrap = true;
                    textStyle.richText = true;
                    textStyle.fontStyle = FontStyle.Normal;
                    GUILayout.Space(10);
                    GUILayout.Label(description, textStyle);
                    GUILayout.Space(10);
                }
                return showDescription;
            }
            else
            {
                return false;
            }


        }

        public static void usualEnd()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        public static void LogoOnlyEnd()
        {

            EditorGUILayout.EndVertical();
        }


        public static void Header(string title, GUIStyle style)
        {

            Rect rectt = EditorGUILayout.BeginVertical();
            //rectt.y = rectt.y - 2f;
            //rectt.width = rectt.width + 6;
            //rectt.x = rectt.x - 3;
            rectt.width = rectt.width + 20;
            rectt.x = 0;
            //EditorUtils.DrawUILineFullWidth(Color.black, thickness: 1, padding: -3);

            Color lightBlue = new Color(1.5f, 1.6f, 1.7f, 2);
            EditorUtils.DrawUILineBottom(rectt, new Color(0.25f, 0.25f, 0.25f, 1f));



            var c = GUI.color;
            if (EditorGUIUtility.isProSkin)
            {
                GUI.color = new Color(0.5f, 1.5f, 2.5f, 1f);
                GUI.color = new Color(1.5f, 1.6f, 1.7f, 2);
                GUI.color = Color.black;
                GUI.color = new Color(0.5f, 1.5f, 2.5f, 1.5f);
            }
            else
            {
                GUI.color = new Color(0.5f, 0.7f, 0.9f, 1f);
            }


            Color myStyleColor = new Color(0.8f, 0.95f, 1f, 1f);// *0.9f;
            myStyleColor = Color.white;
            style.fontStyle = FontStyle.BoldAndItalic;
            style.fontSize = 14;

            style.normal.textColor = myStyleColor;
            style.onNormal.textColor = myStyleColor;
            style.hover.textColor = myStyleColor;
            style.onHover.textColor = myStyleColor;
            style.focused.textColor = myStyleColor;
            style.onFocused.textColor = myStyleColor;
            style.active.textColor = myStyleColor;
            style.onActive.textColor = myStyleColor;

            GUI.Box(rectt, GUIContent.none);
            GUI.color = c;
            GUILayout.Space(5);
            GUILayout.Label(title, style);
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();

            //Color color = Color.black;
            //Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            //r.y = r.y - 4f;
            //r.width = r.width + 6;
            //r.x = r.x - 3;
            //EditorGUI.DrawRect(r, color);

            lightBlue = new Color(1.1f, 1.2f, 1.7f, 0.5f);
            EditorUtils.DrawUILineBottom(rectt, lightBlue, 1);
        }



        public static void DrawUILine(int thickness = 1, int padding = 10)
        {
            Color color = Color.black;
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawUILineFull(int thickness = 1, int padding = 10)
        {
            Color color = Color.black;
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.width = Screen.width;
            r.x = 0;
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }
        public static void DrawUILineFull(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.width = Screen.width;
            r.x = 0;
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }
        public static void DrawUILineGray(int thickness = 2, int padding = 10)
        {
            Color color = new Color(0.2f, 0.2f, 0.2f, 1);
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawUILineLightBlue(int thickness = 1, int padding = 0)
        {
            Color color = new Color(0.5f, 0.6f, 0.7f, 1);
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawUILineFullWidth(Color color, int thickness = 2, int padding = -1)
        {

            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.x = 0;
            r.width += 30;
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }
        public static void DrawUILine(Color color, int thickness = 2, int padding = -1)
        {
            
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawUILineBottom(Rect rect, Color color, int thickness = 2, int padding = -1)
        {

            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x = rect.x;
            r.width = rect.width;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawUILine(Rect rect, Color color, int thickness = 2, int padding = -1)
        {

            Rect r = rect;
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawSubMenuSeparation()
        {
            //EditorGUILayout.Space();
            EditorUtils.DrawUILineSubMenu(1, -1, 10);
            EditorUtils.DrawUILineSubMenu(Color.gray, 1, 2, 10);
        }

        public static void DrawSubMenuSeparation2()
        {
            //EditorGUILayout.Space();
            //Color color = new Color(0.28f, 0.28f, 0.28f, 1);
            //EditorUtils.DrawUILineSubMenu(color, 1, -2, 10);
            EditorUtils.DrawUILineSubMenu(4, -1, 0);
            EditorUtils.DrawUILineSubMenu(Color.gray, 1, 2, 0);
        }

        public static void DrawUILineSubMenu(int thickness = 2, int padding = 1)
        {
            Color color = new Color(0.28f, 0.28f, 0.28f, 1);



            DrawUILineSubMenu(color, thickness, padding);
        }

        public static void DrawUILineSubMenu(int thickness = 2, int padding = 1, float margin = 20)
        {
            Color color = new Color(0.28f, 0.28f, 0.28f, 1);



            DrawUILineSubMenu(color, thickness, padding, margin);
        }

        public static void DrawUILineSubMenu(Color color, int thickness = 2, int padding = 1)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            //r.y += padding / 2;
            float marginLeft = 20;
            r.width = r.width - marginLeft;
            r.x += marginLeft;

            EditorGUI.DrawRect(r, color);
        }

        public static void DrawUILineSubMenu(Color color, int thickness = 2, int padding = 1, float margin = 20)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            //r.y += padding / 2;
            float marginLeft = margin;
            r.width = r.width - marginLeft;
            r.x += marginLeft;

            EditorGUI.DrawRect(r, color);
        }
        public static void DrawUILineCenter(Color color, int thickness = 2, int padding = 1)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            //r.y += padding / 2;
            float marginLeft = 20;
            r.width = r.width - marginLeft * 2;
            r.x += marginLeft;

            EditorGUI.DrawRect(r, color);
        }


        public static void makeHorizontalSeparation(Color color)
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 10;

            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
        }

        public static void makeHorizontalSeparation()
        {
            makeHorizontalSeparation(new Color(0.4f, 0.4f, 0.4f));
        }

        public static void DrawBox(Rect position, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.Box(position, GUIContent.none);
            GUI.color = oldColor;
        }
    }
}