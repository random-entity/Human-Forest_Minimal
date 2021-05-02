using UnityEngine;

public class Person : MonoBehaviour
{
    public bool isAlive;

    public float Emotion, Health;
    public ValueSystem Values;
    public Vector2 Position;

    public float Happiness;

    private void Awake()
    {
        Position = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)) * GameManager.instance.LandSize;
    }

    private void Update()
    {
        SetTransformToPositionVector();
    }

    public (PPAction DesiredPPAction, Person obj) GetDesiredPPAction()
    {
        PPAction desire = BehaviorManager.instance.Idle;
        Person desireObject = null;
        float max = Mathf.NegativeInfinity;

        foreach (PPAction pPAction in BehaviorManager.instance.PPActionList)
        {
            foreach (Person obj in SocietyManager.instance.RealSociety)
            {
                Person imaginedSelf = Instantiate(this);
                Person imaginedOther = Instantiate(obj);

                float selfDeltaEmotion = pPAction.EstimateDeltaEmotionSub(imaginedSelf, imaginedOther);
                if (max < selfDeltaEmotion)
                {
                    max = selfDeltaEmotion;
                    desire = pPAction;
                    desireObject = obj;
                }
            }
        }

        return (desire, desireObject);
    }

    public PPAction GetEthicalPPAction()
    {
        return null;
    }

    public float GetHappiness()
    {
        float happiness = 0f;

        float reputation = 0f;
        float othersEmotion = 0f;
        int aliveCount = 0;
        foreach (Person obj in SocietyManager.instance.RealSociety)
        {
            if (obj.isAlive)
            {
                aliveCount++;

                reputation += SocietyManager.instance.DirectionalExpectedEmotions[new PersonPair(this, obj)];

                othersEmotion += obj.Emotion * SocietyManager.instance.DirectionalEmotions[new PersonPair(this, obj)];
            }
        }
        reputation /= (float)aliveCount;
        othersEmotion /= (float)aliveCount;

        happiness = Values.WeightEmotion * Emotion + Values.WeightHealth * Health + Values.WeightReputation * reputation + Values.WeightKindness * othersEmotion;

        return happiness;
    }

    private void SetTransformToPositionVector()
    {
        transform.position = new Vector3(Position.x, 1, Position.y);
    }
}