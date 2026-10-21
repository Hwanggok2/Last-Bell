using UnityEngine;

namespace LastBell
{
    public sealed class ActorView : MonoBehaviour
    {
        public SpriteRenderer Body;
        public Sprite[] IdleFrames;
        public Sprite[] AttackFrames;
        public Sprite[] HurtFrames;
        public Sprite[] StunFrames;
        public Sprite[] DodgeFrames;
        public Vector3 SpriteOffset = new Vector3(0,.8f,0);
        float age,poseTime;
        int pose,direction;
        bool stunned,facingRight=true;
        Vector3 baseScale=Vector3.one;
        ActorSprites art;
        Color normalColor=Color.white;
        public void PlayDodge(int value){pose=1;direction=value;poseTime=.26f;}
        public void PlayAttack(){pose=2;poseTime=.28f;}
        public void PlayHit(){pose=3;poseTime=.18f;}
        public void SetStunned(bool value){stunned=value;}
        public void ApplyArt(ActorSprites value,bool faceRight)
        {
            if(value==null||value==art)return;art=value;facingRight=faceRight;
            IdleFrames=value.idle;AttackFrames=value.attack;StunFrames=value.stun;
            if(IdleFrames.Length>0&&Body)
            {
                Body.sprite=IdleFrames[0];float height=Mathf.Max(.01f,Body.sprite.bounds.size.y);
                baseScale=Vector3.one*(value.height/height);SpriteOffset=Vector3.zero;Body.flipX=value.facesRight!=faceRight;Body.transform.localPosition=SpriteOffset;Body.transform.localScale=baseScale;
            }
        }
        void LateUpdate()
        {
            if(!Body)return;
            age+=Time.unscaledDeltaTime;poseTime=Mathf.Max(0,poseTime-Time.unscaledDeltaTime);if(poseTime==0)pose=0;
            Vector3 offset=SpriteOffset;Vector3 scale=Vector3.one;
            if(pose==1){float k=Mathf.Sin((1-poseTime/.26f)*Mathf.PI);if(direction==0)offset.y+=.7f*k;else if(direction==1)offset.x-=.45f*k;else if(direction==3)offset.x+=.45f*k;else scale.y=1-.35f*k;}
            else if(pose==2)
            {
                float elapsed=.28f-poseTime;
                // Presentation follows the 100ms core windup; no hit or damage is decided here.
                float reach=elapsed<.1f?elapsed/.1f:1-(elapsed-.1f)/.18f;
                offset.x+=(facingRight?.35f:-.16f)*Mathf.Sin(Mathf.Clamp01(reach)*Mathf.PI*.5f);
            }
            else offset.y+=Mathf.Floor(Mathf.Sin(age*3)*1.5f)/100;
            Body.transform.localPosition=offset;Body.transform.localScale=Vector3.Scale(scale,baseScale);
            Body.color=pose==3?new Color(1,.55f,.35f):normalColor;
            var dodge=art==null?DodgeFrames:direction==0?art.dodgeUp:direction==1?art.dodgeBack:direction==2?art.dodgeDown:art.dodgeForward;
            var frames=pose==1?dodge:pose==2?AttackFrames:pose==3?HurtFrames:stunned?StunFrames:IdleFrames;
            if(frames!=null&&frames.Length>0)
            {
                int frame=pose==2?(.28f-poseTime<.1f?0:Mathf.Min(1,frames.Length-1)):(int)(age*8)%frames.Length;
                Body.sprite=frames[frame];
            }
            Body.transform.localRotation=Quaternion.Euler(0,0,stunned?Mathf.Sin(age*14)*6:0);
        }
    }
}
