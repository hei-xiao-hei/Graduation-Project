using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Focus : MonoBehaviour
{
    public Transform PlayerPos;//玩家位置
    public Transform TeacherPos;//老师的位置
    private float FocusRadius;//专注力范围半径
    private float Countdown;//进入专注力的时间
    private bool Focusing;//是否需要专注

    //专注力滑条
    public Slider FocusSlider;//专注力进度条
    public float speed;
    public GameObject BG;//专注力UI和文本

    //专注力声音
    public AudioSource FocusSource;

    //警告UI
    public GameObject WarningUI;//警告UI
    private float Warningvalue;//警告值

    //进入专注模式提示
    public GameObject FocusingUI;//进入专注模式UI

    //开始进行沟通训练
    public bool isTraining = false;

    public static Focus focus;
    public GameObject GameManager;//脚本

    // Start is called before the first frame update
    void Start()
    {
        focus = this;
        BG.SetActive(true);//显示
        FocusRadius = 15.0f;
        Warningvalue = 50.0f;
        WarningUI.SetActive(false);
        Countdown = 0;
        Focusing = false;
        FocusingUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //开始进行专注力检测，如果不处于专注模式则开始专注检测
        if(Focusing==false)
        {
            isFocus();
        }
        if(FocusSlider.value<= Warningvalue&& CircleFocus(PlayerPos, TeacherPos, FocusRadius) == false)
        {
            WarningShine(WarningUI);
        }
        //Debug.Log("当前是否处于专注模式：" + CircleFocus(PlayerPos, TeacherPos, FocusRadius));
    }

    //圆形检测，进入专注力提升区间
    private bool CircleFocus(Transform player,Transform teacher, float radius)
    {
        float distance = Vector3.Distance(player.position, teacher.position);
        //Debug.Log("当前的距离是：" + distance);
        if(distance<=radius)
        {
            //Debug.Log("进入专注力区域");
            return true;
        }
        else
        {
            return false;
        }
    }
    //关闭UI和停止老师的召，并进入沟通训练
    private void DelayColseUI()
    {
        FocusingUI.SetActive(false);
        BG.SetActive(false);
        //停止老师的召唤
        FocusSource.loop = false;
        FocusSource.playOnAwake = false;
        StartCoroutine(AudioPlayFinished(FocusSource.clip.length));
        isTraining = true;
        GameManager.GetComponent<Training>().enabled = true;//开启训练脚本

    }
    IEnumerator AudioPlayFinished(float times)
    {
        yield return new WaitForSeconds(times);
        Training.training.TrainBG.SetActive(true);
        Training.training.LoadAnswer();//加载题目
    }

    //专注检测（处于专注力提升的区域，并待了5s）
    private void isFocus()
    {
        //如果处于专注力增加范围
        if (CircleFocus(PlayerPos, TeacherPos, FocusRadius))
        {
            //专注力增加
            if (FocusSlider.value < 100)
            {
                FocusSlider.value += Time.deltaTime * speed*10;
            }
            //老师的声音增加
            if (FocusSource.volume < 1)
            {
                FocusSource.volume += Time.deltaTime * 0.005f;
            }
            WarningUI.SetActive(false);//警告UI关闭
            //如果专注力为100时，开始计时
            if(FocusSlider.value >= 100)
            {
                Countdown += Time.deltaTime;
            }
        }
        else
        {
            //专注值减少
            if (FocusSlider.value > 0)
            {
                FocusSlider.value -= Time.deltaTime * speed;
                
            }
            //老师说话声音降低，进入自己小世界
            if (FocusSource.volume > 0)
            {
                FocusSource.volume -= Time.deltaTime * 0.005f;
            }
            Countdown = 0;//计时器清零。
        }

        if(FocusSlider.value>=100&&Countdown>=2)
        {
            Focusing = true;//处于专注模式
            //显示进入专注力模式的UI
            FocusingUI.SetActive(true);
            Invoke(nameof(DelayColseUI), 2.0f);//2秒后提示文本
            
        }
    }

    //警告UI闪烁
    private void WarningShine(GameObject warningUI)
    {
        StartCoroutine(WarningUIDelay(warningUI));
    }
    IEnumerator WarningUIDelay(GameObject warningUI)
    {
        yield return new WaitForSeconds(1.0f);
        warningUI.gameObject.SetActive(!warningUI.gameObject.activeInHierarchy);
    }
}
