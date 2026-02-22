import { useState, useRef, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Input,
  Label,
  Switch,
  Dropdown,
  Option,
  Spinner,
  type OptionOnSelectData,
} from '@fluentui/react-components';
import { useAuth } from '../../auth/AuthContext.tsx';
import { authApi, siteApi } from '../../api/endpoints.ts';
import { setAuthToken } from '../../api/apiClient.ts';
import { getTabletCache } from '../../hooks/useLocalStorage.ts';
import type { LoginConfigResponse } from '../../types/api.ts';
import type { Plant } from '../../types/domain.ts';
import qscLogo from '../../assets/qsc-logo.png';
import styles from './LoginScreen.module.css';

export function LoginScreen() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const empInputRef = useRef<HTMLInputElement>(null);

  const [employeeNumber, setEmployeeNumber] = useState('');
  const [pin, setPin] = useState('');
  const [isWelder, setIsWelder] = useState(false);
  const [selectedSiteId, setSelectedSiteId] = useState('');
  const [sites, setSites] = useState<Plant[]>([]);
  const [loginConfig, setLoginConfig] = useState<LoginConfigResponse | null>(null);
  const [configLoading, setConfigLoading] = useState(false);
  const [loginLoading, setLoginLoading] = useState(false);
  const [error, setError] = useState('');
  const [empError, setEmpError] = useState('');
  const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    empInputRef.current?.focus();
  }, []);

  const fetchLoginConfig = useCallback(async (empNo: string) => {
    if (!empNo.trim()) return;
    setConfigLoading(true);
    setEmpError('');
    try {
      const config = await authApi.getLoginConfig(empNo);
      setLoginConfig(config);
      setSelectedSiteId(config.defaultSiteId);
      const siteList = await siteApi.getSites();
      setSites(siteList);
    } catch (err: unknown) {
      const msg = (err as { message?: string })?.message;
      const isBacked = msg && !msg.startsWith('Request failed');
      setEmpError(isBacked ? msg : 'Employee number not recognized.');
      setLoginConfig(null);
    } finally {
      setConfigLoading(false);
    }
  }, []);

  const handleEmployeeNumberChange = useCallback(
    (value: string) => {
      setEmployeeNumber(value);
      setError('');
      setEmpError('');
      setLoginConfig(null);

      if (debounceTimer.current) clearTimeout(debounceTimer.current);
      if (value.trim()) {
        debounceTimer.current = setTimeout(() => {
          fetchLoginConfig(value);
        }, 500);
      }
    },
    [fetchLoginConfig],
  );

  const handleEmployeeBlur = useCallback(() => {
    if (debounceTimer.current) clearTimeout(debounceTimer.current);
    if (employeeNumber.trim() && !loginConfig && !configLoading) {
      fetchLoginConfig(employeeNumber);
    }
  }, [employeeNumber, loginConfig, configLoading, fetchLoginConfig]);

  const handleLogin = useCallback(async () => {
    if (!employeeNumber.trim()) return;
    setLoginLoading(true);
    setError('');
    try {
      const response = await authApi.login({
        employeeNumber,
        pin: loginConfig?.requiresPin ? pin : undefined,
        siteId: selectedSiteId,
        isWelder,
      });
      setAuthToken(response.token);
      login(response.token, response.user, isWelder);

      if (response.user.roleTier < 6) {
        navigate('/menu');
      } else {
        const cache = getTabletCache();
        if (cache) {
          navigate('/operator');
        } else {
          navigate('/tablet-setup');
        }
      }
    } catch {
      setError('Login failed. Check your employee number and PIN.');
    } finally {
      setLoginLoading(false);
    }
  }, [employeeNumber, pin, selectedSiteId, isWelder, loginConfig, login, navigate]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') {
        if (loginConfig?.requiresPin && !pin) {
          document.getElementById('pin-input')?.focus();
        } else if (loginConfig) {
          handleLogin();
        }
      }
    },
    [loginConfig, pin, handleLogin],
  );

  const canLogin = Boolean(loginConfig && employeeNumber.trim());

  return (
    <div className={styles.container}>
      <div className={styles.form}>
        <div className={styles.logoSection}>
          <img src={qscLogo} alt="Quality Steel Corporation" className={styles.logo} />
          <div className={styles.titleBlock}>
            <h1 className={styles.title}>MES Login</h1>
            <span className={styles.version}>{__APP_VERSION__}</span>
          </div>
        </div>

        <div className={styles.welderField}>
          <Label className={styles.label}>Welder</Label>
          <Switch
            checked={isWelder}
            onChange={(_, data) => setIsWelder(data.checked)}
            label={{ children: isWelder ? 'Yes' : 'No', style: { color: '#ffffff' } }}
            className={styles.toggle}
          />
        </div>

        <div className={styles.credentialsRow}>
          <div className={styles.field}>
            <Label htmlFor="emp-input" className={styles.label}>
              Employee No.
            </Label>
            <Input
              id="emp-input"
              ref={empInputRef}
              type="password"
              inputMode="numeric"
              value={employeeNumber}
              onChange={(_, data) => handleEmployeeNumberChange(data.value)}
              onBlur={handleEmployeeBlur}
              onKeyDown={handleKeyDown}
              className={styles.input}
              size="large"
              appearance="outline"
            />
            {empError && <span className={styles.error}>{empError}</span>}
            {configLoading && <Spinner size="tiny" className={styles.spinner} />}
          </div>

          {loginConfig?.requiresPin && (
            <div className={styles.field}>
              <Label htmlFor="pin-input" className={styles.label}>
                PIN
              </Label>
              <Input
                id="pin-input"
                type="password"
                inputMode="numeric"
                value={pin}
                onChange={(_, data) => setPin(data.value)}
                onKeyDown={handleKeyDown}
                className={styles.input}
                size="large"
                appearance="outline"
              />
            </div>
          )}
        </div>

        <div className={styles.field}>
          <Label htmlFor="site-select" className={styles.label}>
            Site
          </Label>
          <Dropdown
            id="site-select"
            value={sites.find((s) => s.id === selectedSiteId)?.name ?? ''}
            selectedOptions={[selectedSiteId]}
            onOptionSelect={(_, data: OptionOnSelectData) => {
              if (data.optionValue) setSelectedSiteId(data.optionValue);
            }}
            disabled={!loginConfig?.allowSiteSelection}
            className={styles.dropdown}
            size="large"
          >
            {sites.map((site) => (
              <Option key={site.id} value={site.id}>
                {site.name}
              </Option>
            ))}
          </Dropdown>
        </div>

        {error && <div className={styles.loginError}>{error}</div>}

        <Button
          appearance="primary"
          onClick={handleLogin}
          disabled={!canLogin || loginLoading}
          className={styles.loginButton}
          size="large"
        >
          {loginLoading ? <Spinner size="tiny" /> : 'Login'}
        </Button>
      </div>
    </div>
  );
}
