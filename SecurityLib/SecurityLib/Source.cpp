#include <Windows.h>
#include <AclAPI.h>
#include <string>

#ifndef _SECURITYLIB
#define _SECURITYLIB
extern "C" __declspec(dllexport) bool SetRegKeyAdvanced(char* serviceName, int value);
#endif

void setPrivilege(LPCSTR privilege, bool allow) {
	HANDLE token = 0;
	TOKEN_PRIVILEGES tokenPrivileges;
	OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &token);

	if (allow)
	{
		LUID luid;
		LookupPrivilegeValueA(NULL, privilege, &luid);
		tokenPrivileges.PrivilegeCount = 1;
		tokenPrivileges.Privileges[0].Luid = luid;
		tokenPrivileges.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
	}

	AdjustTokenPrivileges(token, false, &tokenPrivileges, sizeof(TOKEN_PRIVILEGES), NULL, NULL);
	CloseHandle(token);
}

bool StartingService(char* serviceName) {
	SC_HANDLE hSCM = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);
	SC_HANDLE hService;
	SERVICE_STATUS Status;

	hService = OpenService(hSCM, serviceName, SERVICE_START | SERVICE_QUERY_STATUS);
	StartService(hService, 0, NULL);

	for (;;) {
		if (!QueryServiceStatus(hService, &Status)) {
			CloseServiceHandle(hService);
			return false;
		}

		if (Status.dwCurrentState != SERVICE_START_PENDING)
			break;

		DWORD dwWait = Status.dwWaitHint;
		if (dwWait == 0)
			dwWait = 1000;

		Sleep(dwWait);
	}

	CloseServiceHandle(hService);
	return true;
}

bool StoppingService(char* serviceName) {
	SC_HANDLE hSCM = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);
	SC_HANDLE hService;
	SERVICE_STATUS Status;
	DWORD dwWait, dwStart, dwTimeout = 120000, dwCheckPoint = (DWORD)-1;


	hService = OpenService(hSCM, serviceName, SERVICE_STOP | SERVICE_QUERY_STATUS);
	ControlService(hService, SERVICE_CONTROL_STOP, &Status);

	dwStart = GetTickCount();

	while (Status.dwCurrentState != SERVICE_STOPPED) {
		// ���������, �� ����� �� �������
		if (dwTimeout != INFINITE) {
			if (GetTickCount() - dwStart >= dwTimeout) {
				CloseServiceHandle(hService);
				return false;
			}
		}

		// ���������� �������� �������� �� ��������� ��������
		// ���������
		if (dwCheckPoint != Status.dwCheckPoint) {
			dwCheckPoint = Status.dwCheckPoint;
			dwWait = Status.dwWaitHint;
		}
		else
			dwWait = 1000;

		// �����
		Sleep(dwWait);

		// �������� ��������� ������
		if (!QueryServiceStatus(hService, &Status)) {
			CloseServiceHandle(hService);
			return false;
		}
	}

	CloseServiceHandle(hService);
	return true;
}

bool SetRegKeyAdvanced(char* serviceName, int value) {
	bool result;
	std::string strPath = "MACHINE\\SYSTEM\\CurrentControlSet\\Services\\" + std::string(serviceName),
		regOpenKeyPath = "SYSTEM\\CurrentControlSet\\Services\\" + std::string(serviceName);
	LPSTR lpstrPath = const_cast<char *>(strPath.c_str());
	ULONG count;
	HKEY hkey = 0;
	PSID sidAdmin = NULL, old_owner = NULL;
	PSECURITY_DESCRIPTOR psd;
	PACL old_acl = NULL, new_acl = NULL;
	PEXPLICIT_ACCESS_A old_explicitAccess, new_explicitAccess = new EXPLICIT_ACCESS_A();
	SID_IDENTIFIER_AUTHORITY sidNTAuthority = SECURITY_NT_AUTHORITY;

	AllocateAndInitializeSid(&sidNTAuthority, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &sidAdmin);

	new_explicitAccess->grfAccessPermissions = GENERIC_ALL;
	new_explicitAccess->grfAccessMode = SET_ACCESS;
	new_explicitAccess->grfInheritance = NO_INHERITANCE;
	new_explicitAccess->Trustee.TrusteeForm = TRUSTEE_IS_SID;
	new_explicitAccess->Trustee.TrusteeType = TRUSTEE_IS_GROUP;
	new_explicitAccess->Trustee.ptstrName = (LPSTR)sidAdmin;

	SetEntriesInAclA(1, new_explicitAccess, NULL, &new_acl);

	// �������� ������ ACL � ������� ���������
	GetNamedSecurityInfoA(lpstrPath, SE_REGISTRY_KEY, DACL_SECURITY_INFORMATION, NULL, NULL, &old_acl, NULL, &psd);
	GetExplicitEntriesFromAclA(old_acl, &count, &old_explicitAccess);
	GetNamedSecurityInfoA(lpstrPath, SE_REGISTRY_KEY, OWNER_SECURITY_INFORMATION, &old_owner, NULL, NULL, NULL, &psd);

	// ���������� ���������� SE_TAKE_OWNERSHIP_NAME, ������ ��������� � ������� ���������� SE_TAKE_OWNERSHIP_NAME
	setPrivilege((LPCSTR)SE_TAKE_OWNERSHIP_NAME, true);
	SetNamedSecurityInfoA(lpstrPath, SE_REGISTRY_KEY, OWNER_SECURITY_INFORMATION, sidAdmin, NULL, NULL, NULL);
	setPrivilege((LPCSTR)SE_TAKE_OWNERSHIP_NAME, false);

	// ������ ����� ACL
	SetNamedSecurityInfoA(lpstrPath, SE_REGISTRY_KEY, DACL_SECURITY_INFORMATION, NULL, NULL, new_acl, NULL);

	// ������ �������� ��������� � �������
	RegOpenKeyExA(HKEY_LOCAL_MACHINE, regOpenKeyPath.c_str(), 0, KEY_ALL_ACCESS, &hkey);
	RegSetValueExA(hkey, "Start", 0, REG_DWORD, (LPBYTE)&value, sizeof(value));
	RegCloseKey(hkey);

	if (value == SERVICE_DISABLED)
		result = StoppingService(serviceName);
	else
		result = StartingService(serviceName);

	// ���������� ������ ACL
	SetEntriesInAclA(count, old_explicitAccess, NULL, &old_acl);
	SetNamedSecurityInfoA(lpstrPath, SE_REGISTRY_KEY, DACL_SECURITY_INFORMATION, NULL, NULL, old_acl, NULL);

	// ���������� ���������� SE_RESTORE_NAME, ���������� ������� ��������� � ������� ���������� SE_RESTORE_NAME
	setPrivilege((LPCSTR)SE_RESTORE_NAME, true);
	SetNamedSecurityInfoA(lpstrPath, SE_REGISTRY_KEY, OWNER_SECURITY_INFORMATION, old_owner, NULL, NULL, NULL);
	setPrivilege((LPCSTR)SE_RESTORE_NAME, false);

	FreeSid(sidAdmin);
	LocalFree(new_acl);
	LocalFree(old_acl);
	return result;
}
