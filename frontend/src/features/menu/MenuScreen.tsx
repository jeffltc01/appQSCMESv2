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
} from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import styles from './MenuScreen.module.css';

interface MenuTile {
  label: string;
  icon: React.ReactNode;
  minRoleTier: number;
  route: string;
  implemented: boolean;
}

const MENU_TILES: MenuTile[] = [
  { label: 'Product Maintenance', icon: <BoxRegular />, minRoleTier: 3, route: '/menu/products', implemented: true },
  { label: 'User Maintenance', icon: <PeopleRegular />, minRoleTier: 3, route: '/menu/users', implemented: true },
  { label: 'Vendor Maintenance', icon: <BuildingRetailRegular />, minRoleTier: 3, route: '/menu/vendors', implemented: true },
  { label: 'Work Center Config', icon: <SettingsRegular />, minRoleTier: 2, route: '/menu/workcenters', implemented: true },
  { label: 'Defect Codes', icon: <ShieldErrorRegular />, minRoleTier: 3, route: '/menu/defect-codes', implemented: true },
  { label: 'Defect Locations', icon: <LocationRegular />, minRoleTier: 3, route: '/menu/defect-locations', implemented: true },
  { label: 'Asset Management', icon: <BuildingRegular />, minRoleTier: 3, route: '/menu/assets', implemented: true },
  { label: 'Kanban Card Mgmt', icon: <TagRegular />, minRoleTier: 5, route: '/menu/kanban-cards', implemented: true },
  { label: 'Characteristics', icon: <ClipboardTextLtrRegular />, minRoleTier: 3, route: '/menu/characteristics', implemented: true },
  { label: 'Control Plans', icon: <ListRegular />, minRoleTier: 2, route: '/menu/control-plans', implemented: true },
  { label: 'Plant Gear', icon: <TopSpeedRegular />, minRoleTier: 3, route: '/menu/plant-gear', implemented: true },
  { label: "Who's On the Floor", icon: <PeopleAudienceRegular />, minRoleTier: 5, route: '/menu/whos-on-floor', implemented: true },
  { label: 'Annotation Types', icon: <DocumentTextRegular />, minRoleTier: 3, route: '/menu/annotation-types', implemented: true },
  { label: 'Production Lines', icon: <LineHorizontal3Regular />, minRoleTier: 3, route: '/menu/production-lines', implemented: true },
  { label: 'Annotations', icon: <NoteRegular />, minRoleTier: 3, route: '/menu/annotations', implemented: true },
  { label: 'Serial Number Lookup', icon: <SearchRegular />, minRoleTier: 5, route: '/menu/serial-lookup', implemented: true },
  { label: 'Sellable Tank Status', icon: <CheckmarkCircleRegular />, minRoleTier: 4, route: '/menu/sellable-tank-status', implemented: true },
  { label: 'Plant Printers', icon: <PrintRegular />, minRoleTier: 3, route: '/menu/plant-printers', implemented: true },
  { label: 'Change Logs', icon: <DocumentTextRegular />, minRoleTier: 3, route: '/menu/change-logs', implemented: false },
  { label: 'Operator View', icon: <DesktopRegular />, minRoleTier: 5, route: '/tablet-setup', implemented: true },
];

export function MenuScreen() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const roleTier = user?.roleTier ?? 99;

  const visibleTiles = MENU_TILES.filter((t) => roleTier <= t.minRoleTier);

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
          <span className={styles.appTitle}>MES Admin</span>
          <span className={styles.plantCode}>{user?.plantCode ?? ''}</span>
        </div>
        <div className={styles.topBarRight}>
          <span className={styles.userName}>{user?.displayName ?? ''}</span>
          <span className={styles.roleName}>{user?.roleName ?? ''}</span>
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
        <div className={styles.tileGrid}>
          {visibleTiles.map((tile) => (
            <button
              key={tile.label}
              className={`${styles.tile} ${!tile.implemented ? styles.tileDisabled : ''}`}
              onClick={() => handleTileClick(tile)}
              disabled={!tile.implemented}
            >
              <span className={styles.tileIcon}>{tile.icon}</span>
              <span className={styles.tileLabel}>{tile.label}</span>
              {!tile.implemented && <span className={styles.tileBadge}>Coming Soon</span>}
            </button>
          ))}
        </div>
      </main>
    </div>
  );
}
