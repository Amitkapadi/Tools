﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TextureEditor;


[ExecuteInEditMode]
public class HintController : MonoBehaviour {

    public TextMesh text;

    hintStage stage;

    enum hintStage {enableTool, draw, addTool, addTexture, renderTexture, WellDone}

    public GameObject picture;
    public GameObject ship;
    public float timer = 5f;

    void setStage(hintStage st) {
        stage = st;
        string ntext = "Well Done! Remember to save your textures before entering/exiting playmode.";
        string mb = (Application.isPlaying) ? "RIGHT MOUSE BUTTON" : "LEFT MOUSE BUTTON";

            switch (stage) {
            case hintStage.enableTool:
                ntext = "Select the cube with " + mb +" and click 'Use Painter Tool'."; break;
            case hintStage.draw:
                ntext = "Draw on the cube. You can DISABLE TOOL, \n or LOCK EDITING for selected object."; break;
            case hintStage.addTool:
                ntext = "Picture on the right has no tool attached. \n Select it in the hierarchy and \n Click 'Add Component'->'Mesh'->'TextureEditor'"; break;
            case hintStage.addTexture:
                ntext = "Ship on the left has no texture. Select him with " +mb+ " and click 'Create Texture'"; break;
            case hintStage.renderTexture:
                int size = RenderTexturePainter.renderTextureSize;
                ntext = "Change MODE to Render Texture. \n This will enable different option and will use two " + size + "*" + size + " Render Texture buffers for editing. \n" +
                    "When using Render Texture to edit different texture2D, \n pixels will be updated at previous one."; break;
            }

        text.text = ntext;

    }
	// Use this for initialization
	void OnEnable () {
     
       setStage(hintStage.enableTool);

    }

    PlaytimePainter pp;

    PlaytimePainter shipPainter() {
        if (pp == null)
         pp = ship.GetComponent<PlaytimePainter>();
        return pp;
    }
    // Update is called once per frame
    void Update() {
        timer -= Time.deltaTime;

        switch (stage) {

		case hintStage.enableTool:  if (PlaytimeToolComponent.enabledTool == typeof(PlaytimePainter)) {  setStage(hintStage.draw); timer = 3f; } break;
		case hintStage.draw: if (PlaytimeToolComponent.enabledTool != typeof(PlaytimePainter)) { setStage(hintStage.enableTool); break; } if (timer < 0) { setStage(hintStage.addTool); } break;
               case hintStage.addTool: if (picture.GetComponent<PlaytimePainter>() != null) { setStage(hintStage.addTexture); } break;
                 case hintStage.addTexture:
                if ((shipPainter()!= null) && (shipPainter().curImgData != null)) setStage(hintStage.renderTexture); break;
                 case hintStage.renderTexture: if ((shipPainter() != null) && (shipPainter().curImgData != null)
                    && (shipPainter().curImgData.TargetIsRenderTexture())) setStage(hintStage.WellDone); break;
                       
                }

        }

    }

