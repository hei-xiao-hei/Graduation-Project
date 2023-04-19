using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using VRTK;
using Valve.VR;

public class Training : MonoBehaviour
{
    public AudioSource TeacherAudioSource;//老师的AudioSource
    private bool TrainStart;//是否可以开始训练

    //训练开始
    public AudioClip[] TeacherClip;//老师的提问
    //public float[] TeacherTime;//老师每次提问的时间
    public GameObject TrainBG;//训练的对话框
    public Text TText;//老师说话的Text组件

    //读取文档
    string[][] ArrayX;
    string[] lineArray;
    private int topicMax;//最大题目数
    private List<bool> isAnserList = new List<bool>();

    //加载题目
    public Text tipsText;//提示信息
    public List<Button> buttonList;//答题的按钮
    public Text TM_Text;//当前题目
    public List<Text> DA_TextList;//选项
    public int topicIndex = 0;//第几题
    public bool isRight;//是否选对了

    //单例模式
    public static Training training;

    
    
    void Start()
    {
        //监听按钮点击事件
        buttonList[0].onClick.AddListener(() => AnswerRightRrongJudgment(1));
        buttonList[1].onClick.AddListener(() => AnswerRightRrongJudgment(2));

        training = this;
        TrainBG.SetActive(false);
        TextCsv();//读取txt数据
        
        isRight = false;
        
    }
    

    //读取txt数据
    void TextCsv()
    {
        //读取csv二进制文件
        TextAsset binAsset = Resources.Load("TrainText", typeof(TextAsset)) as TextAsset;
        //读取每一行的内容
        lineArray = binAsset.text.Split('\r');
        //创建二维数组
        ArrayX = new string[lineArray.Length][];
        //把csv中的数据存储在二维数组中，以：为分割存储数据
        for (int i = 0; i < lineArray.Length; i++)
        {
            ArrayX[i] = lineArray[i].Split(':');
        }
        //设置题目状态
        topicMax = lineArray.Length;
        for (int x = 0; x < topicMax; x++)
        {
            isAnserList.Add(false);
        }
    }

    //加载题目
    public void LoadAnswer()
    {
        isRight = false;//没选答案时，默认为false
        tipsText.text = "";//提示为空
        TM_Text.text = ArrayX[topicIndex][1];//题目
        //老师讲话音频赋值和播放
        TeacherAudioSource.clip= TeacherClip[topicIndex];
        TeacherAudioSource.Play();

        for (int x = 0; x < 2; x++)
        {
            DA_TextList[x].text = ArrayX[topicIndex][x + 2];//选项
        }
    }
    //题目对错判断
    public void AnswerRightRrongJudgment(int index)
    {
        //判断题目对错
        int idx = ArrayX[topicIndex].Length - 1;//XX.Length返回的是元素的个数从1开始计算，而数组的下标从零开始所以获取每行最后一个元素下标则是，改行的元素个数减一。
        int n = int.Parse(ArrayX[topicIndex][idx]) ;//获取正确答案，并转为数字
        Debug.Log("正确答案是：" + n);
        if (n == index)
        {
            tipsText.text = "真棒选对了，按下握持键，将选项读出来吧。";
            isRight= true;
        }
        else
        {
            tipsText.text = "再仔细思考一下。";
            isRight= false;
        }
        Debug.Log("当前选中的是：" + index);
    }

    // Update is called once per frame
    void Update()
    {

    }
    //重复播放老师讲话
    public void Again()
    {
        TeacherAudioSource.Play();//播放老师讲话
    }
    //答对下一题
    public void NextQ()
    {
        topicIndex++;//下一题
        LoadAnswer();//加载题目
    }

}

