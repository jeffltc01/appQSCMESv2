import { useCallback, useEffect, useRef, useState } from 'react';
import { Button, Input, Select, Spinner } from '@fluentui/react-components';
import { SearchRegular } from '@fluentui/react-icons';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { siteApi, whereUsedApi } from '../../api/endpoints.ts';
import type { Plant, WhereUsedResult } from '../../types/domain.ts';
import { formatDateOnly } from '../../utils/dateFormat.ts';
import styles from './WhereUsedScreen.module.css';

interface AppliedFilters {
  heatNumber?: string;
  coilNumber?: string;
  lotNumber?: string;
  plantLabel?: string;
}

export function WhereUsedScreen() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const canChangeSite = (user?.roleTier ?? 99) <= 2;
  const autoSearched = useRef(false);

  const [sites, setSites] = useState<Plant[]>([]);
  const [siteId, setSiteId] = useState(searchParams.get('siteId') ?? user?.defaultSiteId ?? '');
  const [heatNumber, setHeatNumber] = useState(searchParams.get('heatNumber') ?? '');
  const [coilNumber, setCoilNumber] = useState(searchParams.get('coilNumber') ?? '');
  const [lotNumber, setLotNumber] = useState(searchParams.get('lotNumber') ?? '');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [data, setData] = useState<WhereUsedResult[]>([]);
  const [hasSearched, setHasSearched] = useState(false);
  const [appliedFilters, setAppliedFilters] = useState<AppliedFilters>({});

  useEffect(() => {
    if (canChangeSite) {
      siteApi.getSites().then(setSites).catch(() => {});
    }
  }, [canChangeSite]);

  const hasAnySearchValue = heatNumber.trim().length > 0 || coilNumber.trim().length > 0 || lotNumber.trim().length > 0;

  const handleSearch = useCallback(async (
    overrideHeatNumber?: string,
    overrideCoilNumber?: string,
    overrideLotNumber?: string,
    overrideSiteId?: string,
  ) => {
    const nextHeat = (overrideHeatNumber ?? heatNumber).trim();
    const nextCoil = (overrideCoilNumber ?? coilNumber).trim();
    const nextLot = (overrideLotNumber ?? lotNumber).trim();
    const nextSiteId = (overrideSiteId ?? siteId).trim();

    if (!nextHeat && !nextCoil && !nextLot) return;

    const queryParams: Record<string, string> = {};
    if (nextHeat) queryParams.heatNumber = nextHeat;
    if (nextCoil) queryParams.coilNumber = nextCoil;
    if (nextLot) queryParams.lotNumber = nextLot;
    if (nextSiteId) queryParams.siteId = nextSiteId;
    setSearchParams(queryParams, { replace: true });

    setLoading(true);
    setError('');
    setData([]);
    setHasSearched(true);
    const selectedSite = sites.find((s) => s.id === nextSiteId);
    setAppliedFilters({
      heatNumber: nextHeat || undefined,
      coilNumber: nextCoil || undefined,
      lotNumber: nextLot || undefined,
      plantLabel: nextSiteId
        ? (selectedSite ? `${selectedSite.name} (${selectedSite.code})` : (user?.plantName ?? undefined))
        : undefined,
    });
    try {
      const result = await whereUsedApi.search({
        heatNumber: nextHeat || undefined,
        coilNumber: nextCoil || undefined,
        lotNumber: nextLot || undefined,
        siteId: nextSiteId || undefined,
      });
      setData(result);
    } catch {
      setError('Failed to load where used results.');
    } finally {
      setLoading(false);
    }
  }, [coilNumber, heatNumber, lotNumber, setSearchParams, siteId, sites, user?.plantName]);

  useEffect(() => {
    const qsHeat = searchParams.get('heatNumber') ?? '';
    const qsCoil = searchParams.get('coilNumber') ?? '';
    const qsLot = searchParams.get('lotNumber') ?? '';
    const qsSiteId = searchParams.get('siteId') ?? '';
    if ((qsHeat || qsCoil || qsLot) && !autoSearched.current) {
      autoSearched.current = true;
      handleSearch(qsHeat, qsCoil, qsLot, qsSiteId);
    }
  }, [handleSearch, searchParams]);

  return (
    <AdminLayout title="Where Used">
      <div className={styles.controls}>
        {canChangeSite && (
          <div className={styles.fieldGroup}>
            <label className={styles.fieldLabel}>Plant (optional)</label>
            <Select value={siteId} onChange={(_e, d) => setSiteId(d.value)}>
              <option value="">All Plants</option>
              {sites.map((s) => (
                <option key={s.id} value={s.id}>{s.name} ({s.code})</option>
              ))}
            </Select>
          </div>
        )}

        <div className={styles.fieldGroup}>
          <label className={styles.fieldLabel}>Heat</label>
          <Input
            value={heatNumber}
            onChange={(_e, d) => setHeatNumber(d.value)}
            placeholder="Enter heat number"
          />
        </div>

        <div className={styles.fieldGroup}>
          <label className={styles.fieldLabel}>Coil</label>
          <Input
            value={coilNumber}
            onChange={(_e, d) => setCoilNumber(d.value)}
            placeholder="Enter coil number"
          />
        </div>

        <div className={styles.fieldGroup}>
          <label className={styles.fieldLabel}>Lot</label>
          <Input
            value={lotNumber}
            onChange={(_e, d) => setLotNumber(d.value)}
            placeholder="Enter lot number"
          />
        </div>

        <Button
          appearance="primary"
          icon={<SearchRegular />}
          onClick={() => handleSearch()}
          disabled={loading || !hasAnySearchValue}
        >
          Search
        </Button>
      </div>

      {loading && (
        <div className={styles.emptyState}>
          <Spinner size="medium" label="Loading..." />
        </div>
      )}

      {error && <div className={styles.errorMsg}>{error}</div>}

      {!loading && hasSearched && data.length === 0 && !error && (
        <div className={styles.emptyState}>No finished serial numbers found for this search.</div>
      )}

      {data.length > 0 && (
        <>
          <div className={styles.filterChips} data-testid="where-used-active-filters">
            {appliedFilters.heatNumber && <span className={styles.filterChip}>Heat: {appliedFilters.heatNumber}</span>}
            {appliedFilters.coilNumber && <span className={styles.filterChip}>Coil: {appliedFilters.coilNumber}</span>}
            {appliedFilters.lotNumber && <span className={styles.filterChip}>Lot: {appliedFilters.lotNumber}</span>}
            {appliedFilters.plantLabel && <span className={styles.filterChip}>Plant: {appliedFilters.plantLabel}</span>}
          </div>
          <table className={styles.statusTable}>
            <thead>
              <tr>
                <th>Plant</th>
                <th>Serial Number</th>
                <th>Production Number</th>
                <th>Tank Size</th>
                <th>Date Completed (Hydro)</th>
              </tr>
            </thead>
            <tbody>
              {data.map((row) => (
                <tr key={`${row.serialNumber}-${row.plant}`}>
                  <td>{row.plant}</td>
                  <td>
                    <button
                      className={styles.serialLink}
                      onClick={() => navigate(`/menu/serial-lookup?serial=${encodeURIComponent(row.serialNumber)}`)}
                      data-testid={`where-used-serial-${row.serialNumber}`}
                    >
                      {row.serialNumber}
                    </button>
                  </td>
                  <td>{row.productionNumber}</td>
                  <td>{row.tankSize}</td>
                  <td>{row.hydroCompletedAt ? formatDateOnly(row.hydroCompletedAt) : '\u2014'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </AdminLayout>
  );
}
