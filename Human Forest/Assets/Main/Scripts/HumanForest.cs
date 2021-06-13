using System;
using System.Collections.Generic;
using UnityEngine;

public class HumanForest : MonoSingleton<HumanForest>
{
    public Person PersonPrefab;
    [SerializeField] private Transform PersonsParent;
    public int InitialPersonCount = 12;
    public List<Person> RealSociety;
    public List<string> PersonNames;

    public Dictionary<Person, Dictionary<Person, Person>> PsImageOfQs; // ImageMatrix[p][q] = RealPerson p => RealPerson q => ImagePerson p.Image(q).
    public List<Person> RealAndImagesSociety; // 초기화 때를 위해 편의상 전체에 대한 레퍼런스 남겨놓으려고.
    [SerializeField] private Material imagePersonMaterial;

    #region Utilities
    // 히힝
    private float rand()
    {
        return UnityEngine.Random.Range(0.1f, 0.9f);
    }

    private int randInt()
    {
        return UnityEngine.Random.Range(0, InitialPersonCount);
    }

    private List<string> getRandomNames()
    {
        string[] names = PersonalInformations.Names;

        List<string> chosenNames = new List<string>();

        while (chosenNames.Count < InitialPersonCount)
        {
            int choice = randInt();
            string name = names[choice];

            if (!chosenNames.Contains(name)) chosenNames.Add(name);
        }

        return chosenNames;
    }
    #endregion

    #region 관계망 data structuring
    /*
    ** RelationalMatter에 대한 state와 value는 ImagePerson끼리만 가질 수 있다. (물론 p.Image(p)는 자기 자신, ImagePerson이지만 RealPerson.)
    ** RealPerson p가 RealPerson q에게 갖는 RelationalMatter 그런 건 허용하지 않는다는 이야기.
    ** 그런 것을 포함한 모든 것은 RealPerson p의 ImageSociety 속에서만 일어나게끔 하는 것이 덜 복잡하다.
    */

    // public Dictionary<Person, Dictionary<Person, Dictionary<Person, Dictionary<Relation, float>>>> PQRrM2State; // PQRrM2S[p][q][r][rm] = p.Image(q)가 p.Image(r)에게 갖는 rm의 state.
    // public Dictionary<Person, Dictionary<Person, Dictionary<Person, Dictionary<Relation, float>>>> PQRrM2Value; // PQRrM2S[p][q][r][rm] = p.Image(q)가 p.Image(r)에게 갖는 rm의 value.

    // public Dictionary<Person, Dictionary<Matter, float>> PM2State; // RealOrImagePerson p => M2Float (Matter m => float state). p의 s 함수.
    // public Dictionary<Person, Dictionary<Matter, float>> PM2Value; // RealOrImagePerson p => M2Float (Matter m => float state). p의 sigma 함수.

    public Dictionary<Person, Dictionary<Person, float>> PQ2C; // RealPerson p => (RealPerson q => float consideration). p의 c 함수.

    // 이렇게 실수쌍으로 묶어서 해버리면 안 될까? 그러면 값 바뀔 때마다 SVList를 SVDisplay한테 일일이 주지 않아도 되는 거 아니야? 클래스라 레퍼런스 타입이니까 처음 한 번만 레퍼런스 붙여주면 되니까?
    // 이렇게 하면 왜 안 되냐면:
    // s 함수와 sigma 함수가 따로 놀아야 할 때(state는 사람에서 가져오고 sigma는 윤리에서 가져오고 등)가 있어서 Utility 함수에 s와 sigma 따로 넣게 되어 있는데 그럴 때 어려움.
    // => Utility 함수의 오버로드를 만들면 되는 거 아니야?
    public Dictionary<Person, Dictionary<Matter, cloat2>> PM2SV;
    public Dictionary<Person, Dictionary<Person, Dictionary<Person, Dictionary<Relation, cloat2>>>> PQRrM2SV;
    #endregion

    #region Relation-Dependent Matters' State 계산

    #endregion

    #region Utility 계산
    // SSigma2Float U = (M2Float s) => (M2Float sigma) => (float u).

    // 정의 대로 계산: s와 sigma가 분리되어 있을 때를 포함한 가장 일반적인 식.
    public float Utility(Dictionary<Matter, float> s, Dictionary<Matter, float> sigma)
    {
        float u = 0f;
        foreach (Matter m in Enum.GetValues(typeof(Matter)))
        {
            float state = s[m];
            float value = sigma[m];

            u += state * value;
        }
        return u;
    }

    // s와 sigma가 한 Dictionary의 cloat2로 묶여 있을 때.
    public float Utility(Dictionary<Matter, cloat2> m2sv)
    {
        float u = 0f;
        foreach (Matter m in Enum.GetValues(typeof(Matter)))
        {
            float state = m2sv[m].x;
            float value = m2sv[m].y;

            u += state * value;
        }
        return u;
    }

    // Utilities.SplitDictionary를 활용해주세요.

    // p가 q의 Utility를 Image(q)의 가치관에 따라 계산 : true
    // p가 q의 Utility를 p 자신의 가치관에 따라 계산 : false
    public float Utility(Person evaluator, Person target, bool isConsiderateOfTargetsValues) // RealPerson evaluator의 이미지 속 ImagePerson image[target]의 Utility
    {
        Person image = PsImageOfQs[evaluator][target];

        if (isConsiderateOfTargetsValues)
        {
            return Utility(PM2SV[image]);
        }
        else
        {
            return Utility(Extensions.SplitDictionary<Matter>(PM2SV[image], true), Extensions.SplitDictionary<Matter>(PM2SV[evaluator], false));
        }
    }

    public float Utility(Person selfEvaluator) // p가 스스로의 Utility를 계산할 때
    {
        return Utility(selfEvaluator, selfEvaluator, true); // p의 이미지 속 image[p] = (RealPerson) p니까.
    }
    #endregion

    #region Total Utility 계산
    public float TotalUtility(Dictionary<Person, float> c) // 실제 인간들이 갖고 있는 정확한 값을 가지고 주어진 c 함수로 계산할 때.
    {
        float t = 0f;
        foreach (Person p in RealSociety)
        {
            float u = Utility(PM2SV[p]);
            t += c[p] * u;
        }
        return t;
    }

    public float TotalUtility(Person evaluator, Dictionary<Person, float> c, bool isConsiderateOfTargetsValues) // evaluator가 자신의 이미지 인간들을 윤리에서 주어진 c 함수로 계산할 때.
    {
        float t = 0f;
        foreach (Person target in RealSociety)
        {
            Person image = PsImageOfQs[evaluator][target]; // evaluator의 이미지 속 target 
            float u = Utility(evaluator, image, isConsiderateOfTargetsValues); // 

            t += c[target] * u;
        }
        return t;
    }

    public float TotalUtility(Person evaluator, bool isConsiderateOfTargetsValues) // evaluator가 자신의 이미지 인간들을 자기가 신념으로서 가지고 있는 c 함수로 계산할 때.
    {
        return TotalUtility(evaluator, PQ2C[evaluator], isConsiderateOfTargetsValues);
    }
    #endregion

    #region initialization
    public override void Init()
    {
        #region 이름 정하기 게임
        PersonNames = getRandomNames();
        #endregion

        RealSociety = new List<Person>();

        for (int i = 0; i < InitialPersonCount; i++)
        {
            Person pi = Instantiate(PersonPrefab);
            RealSociety.Add(pi);

            pi.transform.SetParent(PersonsParent);
            pi.gameObject.name = "[P" + i + "]" + PersonNames[i];

            pi.SetIsRealAndImageHolder(true, pi);
        }

        PsImageOfQs = new Dictionary<Person, Dictionary<Person, Person>>();

        foreach (Person p in RealSociety)
        {
            Dictionary<Person, Person> PsImage = new Dictionary<Person, Person>();

            foreach (Person q in RealSociety)
            {
                if (q == p)
                {
                    PsImage.Add(p, p); // 혼란을 줄이기 위해 p's image of p는 p와 reference부터 똑같게 하자.
                }
                else
                {
                    Person psImageOfQ = Instantiate(q);
                    psImageOfQ.gameObject.GetComponent<MeshRenderer>().material = imagePersonMaterial;
                    psImageOfQ.gameObject.name = p.gameObject.name + "'s Image of " + q.gameObject.name;
                    PsImage.Add(q, psImageOfQ);

                    psImageOfQ.SetIsRealAndImageHolder(false, p);
                }
            }

            PsImageOfQs.Add(p, PsImage);
        }

        foreach (Person p in RealSociety)
        {
            float angle = 0f;

            foreach (Person q in RealSociety)
            {
                angle += Mathf.PI * 2f / 12f;

                if (q != p)
                {
                    Transform imageTransform = PsImageOfQs[p][q].transform;

                    imageTransform.position = p.transform.position + new Vector3(Mathf.Cos(angle), 2f, Mathf.Sin(angle));
                    imageTransform.localScale = p.transform.localScale * 0.25f;
                    imageTransform.SetParent(p.transform);
                }
            }
        }

        RealAndImagesSociety = new List<Person>();

        foreach (Person p in RealSociety)
        {
            foreach (Person q in RealSociety)
            {
                RealAndImagesSociety.Add(PsImageOfQs[p][q]);
            }
        }

        // PM2State = new Dictionary<Person, Dictionary<Matter, float>>();
        // PM2Value = new Dictionary<Person, Dictionary<Matter, float>>();
        PM2SV = new Dictionary<Person, Dictionary<Matter, cloat2>>();
        foreach (Person p in RealAndImagesSociety)
        {
            // Dictionary<Matter, float> m2s = new Dictionary<Matter, float>();
            // Dictionary<Matter, float> m2v = new Dictionary<Matter, float>();
            Dictionary<Matter, cloat2> m2sv = new Dictionary<Matter, cloat2>();
            foreach (Matter m in Enum.GetValues(typeof(Matter)))
            {
                // m2s.Add(m, 0.5f);
                // m2v.Add(m, 1f);
                m2sv.Add(m, new cloat2(rand(), rand()));
            }

            // PM2State.Add(p, m2s);
            // PM2Value.Add(p, m2v);
            PM2SV.Add(p, m2sv);
        }

        // PQRrM2State = new Dictionary<Person, Dictionary<Person, Dictionary<Person, Dictionary<Relation, float>>>>();
        // PQRrM2Value = new Dictionary<Person, Dictionary<Person, Dictionary<Person, Dictionary<Relation, float>>>>();
        PQRrM2SV = new Dictionary<Person, Dictionary<Person, Dictionary<Person, Dictionary<Relation, cloat2>>>>();
        foreach (Person p in RealSociety)
        {
            // Dictionary<Person, Dictionary<Person, Dictionary<Relation, float>>> qRrM2State = new Dictionary<Person, Dictionary<Person, Dictionary<Relation, float>>>();
            // Dictionary<Person, Dictionary<Person, Dictionary<Relation, float>>> qRrM2Value = new Dictionary<Person, Dictionary<Person, Dictionary<Relation, float>>>();
            Dictionary<Person, Dictionary<Person, Dictionary<Relation, cloat2>>> qRrM2SV = new Dictionary<Person, Dictionary<Person, Dictionary<Relation, cloat2>>>();

            foreach (Person q in RealSociety)
            {
                Person psImageOfQ = PsImageOfQs[p][q];
                // Dictionary<Person, Dictionary<Relation, float>> rrM2State = new Dictionary<Person, Dictionary<Relation, float>>();
                // Dictionary<Person, Dictionary<Relation, float>> rrM2Value = new Dictionary<Person, Dictionary<Relation, float>>();
                Dictionary<Person, Dictionary<Relation, cloat2>> rrM2SV = new Dictionary<Person, Dictionary<Relation, cloat2>>();

                foreach (Person r in RealSociety)
                {
                    Person psImageOfR = PsImageOfQs[p][r];
                    // Dictionary<Relation, float> rM2State = new Dictionary<Relation, float>();
                    // Dictionary<Relation, float> rM2Value = new Dictionary<Relation, float>();
                    Dictionary<Relation, cloat2> rM2SV = new Dictionary<Relation, cloat2>();

                    foreach (Relation rm in Enum.GetValues(typeof(Relation)))
                    {
                        // rM2State.Add(rm, 0.5f);
                        // rM2Value.Add(rm, 1f);
                        rM2SV.Add(rm, new cloat2(rand(), rand()));
                    }

                    // rrM2State.Add(r, rM2State);
                    // rrM2Value.Add(r, rM2State);
                    rrM2SV.Add(r, rM2SV);
                }

                // qRrM2State.Add(psImageOfQ, rrM2State);
                // qRrM2Value.Add(psImageOfQ, rrM2State);
                qRrM2SV.Add(psImageOfQ, rrM2SV);
            }

            // PQRrM2State.Add(p, qRrM2State);
            // PQRrM2Value.Add(p, qRrM2State);
            PQRrM2SV.Add(p, qRrM2SV);
        }
    }
    #endregion
}