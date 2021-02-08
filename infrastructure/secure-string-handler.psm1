Function Convert-SecureStringToAzureSecureString {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [securestring]$SecureStringToConvert
    )

    # During deployment using ARM template and SecureString parameters, the deployment will fail with
    # the error message "Template parameter JToken type is not valid. Expected 'String, Uri'".
    # Despite the parameter value being a SecureString object, the below hack is required to
    # "re-convert" it to a SecureString that will not cause the deployment to fail.
    # Refer to https://github.com/Azure/azure-powershell/issues/12792#issuecomment-706294242.
    $cred = New-Object -TypeName PSCredential -ArgumentList "fakeusername", $SecureStringToConvert
    $cred.Password
}
