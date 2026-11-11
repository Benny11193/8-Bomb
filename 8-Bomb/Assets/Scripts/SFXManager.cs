using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private AudioSource sfxSource;

    [Header("基礎操作音效")]
    public AudioClip moveClip;           // 移動
    public AudioClip pushClip;           // 推動球 (相撞)
    public AudioClip switchPlayerClip;   // 切換主角

    [Header("合成與破壞音效")]
    public AudioClip combineClip;        // 數字合成 (未爆炸)
    public AudioClip destroyBoxClip;     // 破壞紙箱 (拆牆)
    public AudioClip directionalExplosionClip; // 定向爆炸 (1~11號)
    public AudioClip ultimateExplosionClip;    // 大招爆炸 (12~15號)

    [Header("收集與進度音效")]
    public AudioClip collectStarClip;    // 收集星星
    public AudioClip chestAppearClip;    // 寶箱出現

    [Header("系統結算音效")]
    public AudioClip gameOverClip;       // Game Over
    public AudioClip victoryClip;        // 勝利 (Game Complete)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 抓取掛在同一個 GameObject 上的 AudioSource
        sfxSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 內部共用的播放邏輯，自帶防呆檢查
    /// </summary>
    private void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    // ==========================================
    // 公開播放介面 (供其他腳本呼叫)
    // ==========================================
    public void PlayMove() => PlayClip(moveClip);
    public void PlayPush() => PlayClip(pushClip);
    public void PlaySwitchPlayer() => PlayClip(switchPlayerClip);
    
    public void PlayCombine() => PlayClip(combineClip);
    public void PlayDestroyBox() => PlayClip(destroyBoxClip);
    
    public void PlayDirectionalExplosion() => PlayClip(directionalExplosionClip);
    public void PlayUltimateExplosion() => PlayClip(ultimateExplosionClip);
    
    public void PlayCollectStar() => PlayClip(collectStarClip);
    public void PlayChestAppear() => PlayClip(chestAppearClip);
    
    public void PlayGameOver() => PlayClip(gameOverClip);
    public void PlayVictory() => PlayClip(victoryClip);
}
