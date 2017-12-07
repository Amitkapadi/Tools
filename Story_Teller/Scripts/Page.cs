﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using PlayerAndEditorGUI;




namespace StoryTriggerData{
	
    [TagName(Page.storyTag)]
	[ExecuteInEditMode]
    public class Page : STD_Object {

        List<STD_Object> linkedObjects;

        public const string storyTag = "page";


        [NonSerialized]
        public string anotherBook;
        [NonSerialized]
        UniverseLength distance;
        [NonSerialized]
        public float sceneScale;
        public UniversePosition pos = new UniversePosition();
        public UniverseLength radius = new UniverseLength();
        bool objectsLoaded;
        public string OriginBook;


	public override string getDefaultTagName(){
		return storyTag;
	}

        public string GerResourcePath() {
            if ((anotherBook!= null) && (anotherBook.Length > 0))
                return anotherBook;  // Another book or web address

            if (parentPage == null)
                return OriginBook; // this book
            else
                return parentPage.GerResourcePath() + "/" + parentPage.gameObject.name;
       }

    public override void Reboot() {
            if (poolController == null)
                myPoolController.AddToPool(this.gameObject);
            
            gameObject.hideFlags = HideFlags.DontSave;

            anotherBook = "";

            linkedObjects = new List<STD_Object>();

            //if (dist == null)
            distance = new UniverseLength();

            pos = new UniversePosition();
            radius = new UniverseLength(10);

            //Debug.Log("Rebooting page");


            objectsLoaded = false;
    }

        public override void Decode(string tag, string data) {

            switch (tag) {
                case "name": gameObject.name = data; break;
                case "origin": OriginBook = data; break;
                case "URL": anotherBook = data; break;
                case "size": radius = new UniverseLength(data);  break;
                case UniversePosition.storyTag: pos.Reboot(data); break;
                default:
                    STD_Object storyObject = STD_Pool.getOne(tag);

                    if (storyObject != null) {
                        storyObject.LinkTo(this);
                        storyObject.Reboot(data);
                    } else
                        Debug.Log("Unrecognised tag:" + tag);
                    break;
            }
        }

    void encodeMeta(stdEncoder cody) {
            cody.AddText("origin", OriginBook);
            cody.AddText("name", gameObject.name);
            cody.AddIfNotEmpty("URL", anotherBook);
            cody.AddIfNotNull(pos);
            cody.Add("size", radius);
        }



    public override stdEncoder Encode (){ // Page and it's full content is saved in a saparate file
		
			var cody = new stdEncoder();

            encodeMeta(cody);

            return cody;
	}

      

    public stdEncoder EncodeContent() {

            var cody = new stdEncoder();

            encodeMeta(cody);

            foreach (STD_Object sc in linkedObjects) 
              cody.AddIfNotNull (sc);

            return cody;
    }

    

    public void SavePageContent() {
            ResourceSaver.SaveToResources(TriggerGroups.StoriesFolderName, GerResourcePath(), gameObject.name, EncodeContent().ToString());
    }

    public void LoadContent() {
            new stdDecoder(ResourceLoader.LoadStoryFromResource(TriggerGroups.StoriesFolderName, GerResourcePath(), gameObject.name)).DecodeTagsFor(this);
            objectsLoaded = true;
    }

    public void RenamePage(string newName) {
            
            bool duplicate = false;
            foreach (Page p in Book.HOMEpages) {
                if ((p.gameObject.name == gameObject.name) && (p.gameObject != this.gameObject)) { duplicate = true; break; } //
            }

            string path = "Assets/" + TriggerGroups.StoriesFolderName + "/Resources/" + GerResourcePath() + "/";

            if (duplicate)
                UnityHelperFunctions.DuplicateResource(TriggerGroups.StoriesFolderName, GerResourcePath(), gameObject.name, newName);
            else
            UnityEditor.AssetDatabase.RenameAsset(path + gameObject.name + ResourceSaver.fileType, newName + ResourceSaver.fileType);
            
            gameObject.name = newName;
    }

	public override void Deactivate (){

            if (Application.isPlaying == false)
                SavePageContent();

			clearPage ();
            if (parentPage == null)
                Book.HOMEpages.Remove(this);
			base.Deactivate ();
	}

    public void Unlink(STD_Object sb) {
            if (linkedObjects.Remove(sb)) {
                sb.parentPage = null;
                sb.transform.parent = null;
            }
        }

        public void Link(STD_Object sb) {
            if (sb.parentPage != null)
                sb.parentPage.Unlink(sb);
            sb.transform.parent = transform;
            sb.parentPage = this;
            linkedObjects.Add(sb);  
        }

		public void clearPage(){
            for (int i = linkedObjects.Count-1; i >=0 ; i--)
                linkedObjects[i].Deactivate ();

             linkedObjects = new List<STD_Object> ();
		}

    public static Page browsedPage;
	int exploredObject = -1;


	public override void PEGI () {
            
			browsedPage = this;

            if (exploredObject >= 0) {

                if ((linkedObjects.Count <= exploredObject) || ("< Pools".Click(35)))
                    exploredObject = -1;
                else
                linkedObjects[exploredObject].PEGI();

            }

            if (exploredObject == -1) {

                pegi.newLine();

                if (parentPage == null)
                    pegi.write(gameObject.name+":HOME page ");
                else {
                    pegi.write(gameObject.name+" is child of:",60);
                    pegi.write(parentPage);
                }

                (objectsLoaded ? "loaded" : "not loaded").nl(60);

             
                if ("Location: ".edit(60,ref anotherBook).nl()) {
                    if (anotherBook[anotherBook.Length - 1] == '/')
                        anotherBook = anotherBook.Substring(0, anotherBook.Length - 1);
                }
               
            
                if ("Clear".Click())
                    clearPage();

                if ("Load".Click()) {
                    clearPage();
                    LoadContent();
                }

                if ("Save".Click().nl())
                    SavePageContent();


                for(int i = 0; i < STD_Pool.all.Length; i++) {
                    STD_Pool up = STD_Pool.all[i];

                    pegi.write(up.storyTag, 35);
                    pegi.write(up.pool.prefab);

                    if (icon.Add.Click(20))
                        STD_Pool.all[i].pool.getFreeGO().GetComponent<STD_Object>().LinkTo(this).Reboot(null);

                    int Delete = -1;

                    for (int o = 0; o < linkedObjects.Count; o++)  {

                        STD_Object obj = linkedObjects[o];
                        if (obj.poolController == up.pool) {
                            pegi.newLine();

                            if (icon.Delete.Click(20))
                                Delete = o;

                            pegi.edit(linkedObjects[o].gameObject);

                            if (pegi.Click(icon.Edit, "Edit object", 20))
                                exploredObject = o;
                        }
                    }

                    if (Delete != -1)
                        linkedObjects[Delete].Deactivate();
                    
                    pegi.newLine();
                }
            } 
            pegi.newLine();
	}


       


        void Update() {
            float scale;

            transform.position = pos.toV3(radius, out scale, distance);
            transform.localScale = Vector3.one * scale;

            if (!objectsLoaded) // will also check distance to load/unload
                LoadContent();
        }

        public static PoolController<Page> myPoolController;

        public override void SetStaticPoolController(STD_Pool inst) {
            myPoolController = (PoolController<Page>)inst.pool;
        }
       
}
}