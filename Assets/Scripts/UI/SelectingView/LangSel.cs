using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LangSel : MonoBehaviour
{
    public LangSelButton[] LangSelButtons;
    public SystemLanguage DefaultLanguage;

    public UnityEvent<SystemLanguage> OnLanguageChange;

    private void OnEnable()
    {
        SelectedLang = DefaultLanguage;
    }

    public SystemLanguage SelectedLang
    {
        get
        {
            foreach (var btn in LangSelButtons)
            {
                if (btn.Selected)
                    return btn.Language;
            }

            return DefaultLanguage;
        }

        set
        {
            foreach (var btn in LangSelButtons)
                btn.Selected = btn.Language == value;

            OnLanguageChange?.Invoke(value);
        }
    }

    public void ChangeLanguage(LangSelButton btn)
    {
        SelectedLang = btn.Language;
    }
}
