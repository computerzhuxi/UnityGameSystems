using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Computerzhuxi.Navigation2D;

namespace Computerzhuxi.Navigation2D.Samples
{
    /// <summary>演示调用方如何持有路径并驱动简单标记，不依赖任何游戏角色系统。</summary>
    public sealed class NavigationDemo : MonoBehaviour
    {
        [SerializeField] private float radius=.2f;
        private readonly GridPathfinder2D finder=new();
        private readonly List<Vector2> path=new();
        private PhysicsTraversalSource2D source;
        private Transform marker;
        private GameObject wall;
        private Sprite sprite;
        private int index,frames;
        private Vector2 target=new(3,0);
        private PathResult2D result;
        private bool smoke;
        /// <summary>创建演示专用的墙、角色标记与显式障碍源。</summary>
        private void Start()
        {
            smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"--navigation-smoke")>=0;
            sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new(.5f,.5f),Texture2D.whiteTexture.width);
            wall=Visual("Wall",Vector2.zero,new(.5f,3),Color.gray); wall.layer=8; wall.AddComponent<BoxCollider2D>();
            marker=Visual("Agent",new(-3,0),Vector2.one*radius*2,Color.cyan).transform;
            Visual("Goal",target,Vector2.one*.2f,Color.green);
            source=new PhysicsTraversalSource2D(gameObject.scene.GetPhysicsScene2D(),1<<8);
            Physics2D.SyncTransforms(); Repath();
        }
        /// <summary>建立演示可见物体，明确隶属当前场景。</summary>
        private GameObject Visual(string objectName,Vector2 position,Vector2 scale,Color color)
        { var go=new GameObject(objectName); SceneManager.MoveGameObjectToScene(go,gameObject.scene); go.transform.position=position; go.transform.localScale=scale; var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.color=color; return go; }
        /// <summary>从当前标记位置重新查询，输出列表始终由演示控制器持有。</summary>
        private void Repath() { result=finder.FindPath(marker.position,target,new(Vector2.zero,.25f),new(radius,4096),source,path); index=0; }
        /// <summary>检查下一段并推进演示标记；独立冒烟检查真实扫掠结果。</summary>
        private void Update()
        {
            if(marker==null) return;
            if(index<path.Count)
            {
                if(!source.IsSegmentClear(marker.position,path[index],radius)) Repath();
                if(index<path.Count) { marker.position=Vector2.MoveTowards(marker.position,path[index],Time.deltaTime*2); if(Vector2.Distance(marker.position,path[index])<.01f) index++; }
            }
            if(smoke && ++frames==5) { bool pass=result.Succeeded && !source.IsSegmentClear(new(-3,0),target,radius); Debug.Log(pass?"NAVIGATION_SMOKE_PASS":"NAVIGATION_SMOKE_FAIL"); Application.Quit(pass?0:1); }
        }
        /// <summary>提供可手动重复的重置与动态障碍操作。</summary>
        private void OnGUI()
        {
            GUI.Label(new Rect(15,15,700,30),$"Navigation2D | {result.Status} | expanded {result.ExpandedNodes} | radius {radius}");
            if(GUI.Button(new Rect(15,50,170,35),"Reset / Repath")) { marker.position=new Vector2(-3,0); Repath(); }
            if(GUI.Button(new Rect(15,95,170,35),"Toggle obstacle")) { wall.SetActive(!wall.activeSelf); Physics2D.SyncTransforms(); Repath(); }
        }
        /// <summary>绘制完整路径及净空，供人工检查转角和绕障。</summary>
        private void OnDrawGizmos()
        { if(marker==null) return; Vector2 previous=marker.position; Gizmos.color=Color.yellow; for(int i=index;i<path.Count;i++) { Gizmos.DrawLine(previous,path[i]); Gizmos.DrawWireSphere(path[i],radius); previous=path[i]; } }
        /// <summary>释放本组件创建的运行时图形资源。</summary>
        private void OnDestroy() { if(sprite!=null) Destroy(sprite); }
    }
}
