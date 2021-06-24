using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PasswordManager : MonoBehaviour
{
    private InputField passwordField;

    private void Start()
    {
        passwordField = GetComponent<InputField>();
    }

    public void ChangeContentType()
    {
        if (passwordField.contentType == InputField.ContentType.Password)
        {
            passwordField.contentType = InputField.ContentType.Alphanumeric;
            passwordField.ForceLabelUpdate();
            return;
        }
        if (passwordField.contentType == InputField.ContentType.Alphanumeric)
        {
            passwordField.contentType = InputField.ContentType.Password;
            passwordField.ForceLabelUpdate();
            return;
        }
    }
}
