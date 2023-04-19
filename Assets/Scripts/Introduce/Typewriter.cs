using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class Typewriter : MonoBehaviour
{
    //介绍功能实现
    public Text text;//打印内容的文本组件
    public string Content;//文本介绍
    public float PrintTime;//打印的时间


    // Start is called before the first frame update
    void Start()
    {
        text.DOText(Content, PrintTime);
        
    }
    //跳转场景
    public void Next(string NextSceneName)
    {
        SceneManager.LoadScene(NextSceneName);
    }
}
