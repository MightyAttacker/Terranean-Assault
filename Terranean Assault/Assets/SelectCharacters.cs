using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectCharacters : MonoBehaviour
{
    public GameObject[] Characters;
    public int Number;
    public GameObject leftArrow;
    public GameObject rightArrow;

    public void ChangeCharacter(int Num)
    {
        for (int i = 0; i < Characters.Length; i++)
        {
            Characters[i].SetActive(false);
        }

        Number += Num;

        if (Number >= Characters.Length)
        {
            Number = 0;
        }

        if(Number < 0)
        {
            Number = Characters.Length - 1;
        }

        Characters[Number].SetActive(true);
    }
}
