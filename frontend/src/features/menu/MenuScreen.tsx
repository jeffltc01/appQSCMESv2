import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@fluentui/react-components';
import {
  BoxRegular,
  PeopleRegular,
  BuildingRetailRegular,
  SettingsRegular,
  ShieldErrorRegular,
  LocationRegular,
  BuildingRegular,
  TagRegular,
  ClipboardTextLtrRegular,
  ListRegular,
  TopSpeedRegular,
  PeopleAudienceRegular,
  DocumentTextRegular,
  DesktopRegular,
  SignOutRegular,
  LineHorizontal3Regular,
  NoteRegular,
  SearchRegular,
  CheckmarkCircleRegular,
  PrintRegular,
  BugRegular,
  ShieldCheckmarkRegular,
  TableRegular,
  DataBarVerticalRegular,
  ClockRegular,
  BranchRegular,
  CalendarRegular,
  GaugeRegular,
  HistoryRegular,
  TimerRegular,
  ShieldTaskRegular,
} from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { HelpButton } from '../../help/components/HelpButton.tsx';
import { getArticleBySlug } from '../../help/helpRegistry.ts';
import { adminAnnotationApi, frontendTelemetryApi, issueRequestApi } from '../../api/endpoints.ts';
import styles from './MenuScreen.module.css';

interface MenuTile {
  label: string;
  icon: React.ReactNode;
  minRoleTier: number;
  canAccess?: (roleTier: number) => boolean;
  route: string;
  implemented: boolean;
}

interface MenuGroup {
  label: string;
  accentColor: string;
  tiles: MenuTile[];
}

const MENU_GROUPS: MenuGroup[] = [
  {
    label: 'Dashboards & Insights',
    accentColor: '#343a40',
    tiles: [
      { label: 'Supervisor / Team Lead Dashboard', icon: <DataBarVerticalRegular />, minRoleTier: 5, route: '/menu/supervisor-dashboard', implemented: true },
      { label: 'Plant Dashboard', icon: <BranchRegular />, minRoleTier: 4, route: '/menu/digital-twin', implemented: true },
      { label: 'AI Review', icon: <ShieldCheckmarkRegular />, minRoleTier: 2, canAccess: (t) => t <= 2 || t === 5.5, route: '/menu/ai-review', implemented: true },
      { label: "Who's On the Floor", icon: <PeopleAudienceRegular />, minRoleTier: 5, route: '/menu/whos-on-floor', implemented: true },
      { label: 'Serial Number Lookup', icon: <SearchRegular />, minRoleTier: 5, route: '/menu/serial-lookup', implemented: true },
      { label: 'Log Viewer', icon: <TableRegular />, minRoleTier: 7, route: '/menu/production-logs', implemented: true },
    ],
  },
  {
    label: 'Quality & Inspection',
    accentColor: '#e41e2f',
    tiles: [
      { label: 'Defect Codes', icon: <ShieldErrorRegular />, minRoleTier: 3, route: '/menu/defect-codes', implemented: true },
      { label: 'Defect Locations', icon: <LocationRegular />, minRoleTier: 3, route: '/menu/defect-locations', implemented: true },
      { label: 'Characteristics', icon: <ClipboardTextLtrRegular />, minRoleTier: 3, route: '/menu/characteristics', implemented: true },
      { label: 'Control Plans', icon: <ListRegular />, minRoleTier: 3, route: '/menu/control-plans', implemented: true },
      { label: 'Kanban Card Mgmt', icon: <TagRegular />, minRoleTier: 5, route: '/menu/kanban-cards', implemented: true },
      { label: 'Sellable Tank Status', icon: <CheckmarkCircleRegular />, minRoleTier: 4, route: '/menu/sellable-tank-status', implemented: true },
      { label: 'Annotations', icon: <NoteRegular />, minRoleTier: 3, route: '/menu/annotations', implemented: true },
    ],
  },
  {
    label: 'Production & Operations',
    accentColor: '#606ca3',
    tiles: [
      { label: 'Plant Gear', icon: <TopSpeedRegular />, minRoleTier: 3, route: '/menu/plant-gear', implemented: true },
      { label: 'Production Line Work Centers', icon: <SettingsRegular />, minRoleTier: 2, route: '/menu/production-line-workcenters', implemented: true },
      { label: 'Checklist Templates', icon: <ClipboardTextLtrRegular />, minRoleTier: 6, route: '/menu/checklists', implemented: true },
      { label: 'Checklist Response Review', icon: <DataBarVerticalRegular />, minRoleTier: 5, route: '/menu/checklist-response-review', implemented: true },
      { label: 'Checklist Score Types', icon: <ClipboardTextLtrRegular />, minRoleTier: 2, route: '/menu/checklist-score-types', implemented: true },
      { label: 'Downtime Reasons', icon: <ClockRegular />, minRoleTier: 3, route: '/menu/downtime-reasons', implemented: true },
      { label: 'Downtime Log', icon: <TimerRegular />, minRoleTier: 5, route: '/menu/downtime-events', implemented: true },
      { label: 'Shift Schedule', icon: <CalendarRegular />, minRoleTier: 3, route: '/menu/shift-schedule', implemented: true },
      { label: 'Capacity Targets', icon: <GaugeRegular />, minRoleTier: 3, route: '/menu/capacity-targets', implemented: true },
    ],
  },
  {
    label: 'Master Data',
    accentColor: '#2b3b84',
    tiles: [
      { label: 'Product Maintenance', icon: <BoxRegular />, minRoleTier: 3, route: '/menu/products', implemented: true },
      { label: 'Vendor Maintenance', icon: <BuildingRetailRegular />, minRoleTier: 3, route: '/menu/vendors', implemented: true },
      { label: 'Asset Management', icon: <BuildingRegular />, minRoleTier: 3, route: '/menu/assets', implemented: true },
      { label: 'Work Centers', icon: <SettingsRegular />, minRoleTier: 2, route: '/menu/workcenters', implemented: true },
      { label: 'Production Lines', icon: <LineHorizontal3Regular />, minRoleTier: 3, route: '/menu/production-lines', implemented: true },
      { label: 'Annotation Types', icon: <DocumentTextRegular />, minRoleTier: 3, route: '/menu/annotation-types', implemented: true },
    ],
  },
  {
    label: 'Administration',
    accentColor: '#aa121f',
    tiles: [
      { label: 'User Maintenance', icon: <PeopleRegular />, minRoleTier: 3, route: '/menu/users', implemented: true },
      { label: 'Frontend Telemetry', icon: <HistoryRegular />, minRoleTier: 3, route: '/menu/frontend-telemetry', implemented: true },
      { label: 'Audit Log', icon: <HistoryRegular />, minRoleTier: 3, route: '/menu/audit-log', implemented: true },
      { label: 'Plant Printers', icon: <PrintRegular />, minRoleTier: 3, route: '/menu/plant-printers', implemented: true },
      { label: 'Issues', icon: <BugRegular />, minRoleTier: 5, route: '/menu/issues', implemented: true },
      { label: 'Operator View', icon: <DesktopRegular />, minRoleTier: 5, route: '/tablet-setup', implemented: true },
      { label: 'Test Coverage', icon: <ShieldTaskRegular />, minRoleTier: 1, route: '/menu/test-coverage', implemented: true },
      { label: 'Demo Data Tools', icon: <SettingsRegular />, minRoleTier: 1, route: '/menu/demo-data', implemented: true },
    ],
  },
];

export function MenuScreen() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [telemetryWarning, setTelemetryWarning] = useState(false);
  const [pendingIssuesCount, setPendingIssuesCount] = useState(0);
  const [annotationsNeedResponseCount, setAnnotationsNeedResponseCount] = useState(0);

  const roleTier = user?.roleTier ?? 99;
  const isDirectorPlus = roleTier <= 2;

  useEffect(() => {
    if (roleTier > 3) {
      setTelemetryWarning(false);
      return;
    }
    frontendTelemetryApi.getCount(250000)
      .then((result) => setTelemetryWarning(result.isWarning))
      .catch(() => setTelemetryWarning(false));
  }, [roleTier]);

  useEffect(() => {
    if (roleTier > 3) {
      setPendingIssuesCount(0);
      return;
    }
    issueRequestApi.getPending()
      .then((items) => setPendingIssuesCount(items.length))
      .catch(() => setPendingIssuesCount(0));
  }, [roleTier]);

  useEffect(() => {
    if (roleTier > 3) {
      setAnnotationsNeedResponseCount(0);
      return;
    }
    const siteId = isDirectorPlus ? undefined : user?.defaultSiteId;
    adminAnnotationApi.getAll(siteId)
      .then((items) => {
        const unresolved = items.filter((item) => !item.resolvedByName);
        setAnnotationsNeedResponseCount(unresolved.length);
      })
      .catch(() => setAnnotationsNeedResponseCount(0));
  }, [isDirectorPlus, roleTier, user?.defaultSiteId]);

  const handleTileClick = (tile: MenuTile) => {
    if (tile.implemented) {
      navigate(tile.route);
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className={styles.shell}>
      <header className={styles.topBar}>
        <div className={styles.topBarLeft}>
          <span className={styles.appTitle}>QSC MES</span>
          <span className={styles.plantCode}>{user?.plantCode ?? ''}</span>
        </div>
        <div className={styles.topBarRight}>
          <span className={styles.userName}>{user?.displayName ?? ''}</span>
          <span className={styles.roleName}>{user?.roleName ?? ''}</span>
          <HelpButton currentArticle={getArticleBySlug('menu')} className={styles.logoutBtn} />
          <Button
            appearance="subtle"
            icon={<SignOutRegular />}
            className={styles.logoutBtn}
            onClick={handleLogout}
          >
            Logout
          </Button>
        </div>
      </header>

      <main className={styles.content}>
        <div className={styles.groupContainer}>
          {MENU_GROUPS.map((group) => {
            const visibleTiles = group.tiles.filter((t) => t.canAccess ? t.canAccess(roleTier) : roleTier <= t.minRoleTier);
            if (visibleTiles.length === 0) return null;

            return (
              <section key={group.label} className={styles.group}>
                <div className={styles.groupHeader}>
                  <span
                    className={styles.groupAccent}
                    style={{ backgroundColor: group.accentColor }}
                  />
                  <h3 className={styles.groupLabel}>{group.label}</h3>
                </div>
                <div className={styles.tileGrid}>
                  {visibleTiles.map((tile) => (
                    <button
                      key={tile.label}
                      className={`${styles.tile} ${!tile.implemented ? styles.tileDisabled : ''}`}
                      style={{ borderTopColor: group.accentColor }}
                      onClick={() => handleTileClick(tile)}
                      disabled={!tile.implemented}
                    >
                      <span className={styles.tileIcon} style={{ color: group.accentColor }}>
                        {tile.icon}
                      </span>
                      <span className={styles.tileLabel}>{tile.label}</span>
                      {!tile.implemented && <span className={styles.tileBadge}>Coming Soon</span>}
                      {tile.label === 'Frontend Telemetry' && telemetryWarning && (
                        <span className={styles.tileWarnBadge}>Archive Needed</span>
                      )}
                      {tile.label === 'Issues' && roleTier <= 3 && pendingIssuesCount > 0 && (
                        <span
                          className={styles.tileCountBadge}
                          aria-label={`Issues pending approval count: ${pendingIssuesCount}`}
                          title={`Needs Approval: ${pendingIssuesCount}`}
                        >
                          {pendingIssuesCount}
                        </span>
                      )}
                      {tile.label === 'Annotations' && roleTier <= 3 && annotationsNeedResponseCount > 0 && (
                        <span
                          className={styles.tileCountBadge}
                          aria-label={`Annotations needing response count: ${annotationsNeedResponseCount}`}
                          title={`Needs Response: ${annotationsNeedResponseCount}`}
                        >
                          {annotationsNeedResponseCount}
                        </span>
                      )}
                    </button>
                  ))}
                </div>
              </section>
            );
          })}
        </div>
      </main>
    </div>
  );
}
