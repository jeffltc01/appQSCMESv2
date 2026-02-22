export interface HelpArticle {
  slug: string;
  title: string;
  category: 'general' | 'operator' | 'admin';
  routeMatch?: string;
  dataEntryType?: string;
}

export const helpArticles: HelpArticle[] = [
  // ── General ──
  { slug: 'overview', title: 'MES v2 Overview', category: 'general' },
  { slug: 'login', title: 'Login', category: 'general', routeMatch: '/login' },
  { slug: 'tablet-setup', title: 'Tablet Setup', category: 'general', routeMatch: '/tablet-setup' },
  { slug: 'menu', title: 'Admin Menu', category: 'general', routeMatch: '/menu' },
  { slug: 'barcode-reference', title: 'Barcode Command Reference', category: 'general' },

  // ── Operator Screens ──
  { slug: 'operator-layout', title: 'Operator Layout', category: 'operator', routeMatch: '/operator' },
  { slug: 'rolls', title: 'Rolls', category: 'operator', dataEntryType: 'Rolls' },
  { slug: 'rolls-material', title: 'Rolls Material Queue', category: 'operator', dataEntryType: 'MatQueue-Material' },
  { slug: 'long-seam', title: 'Long Seam', category: 'operator', dataEntryType: 'Barcode-LongSeam' },
  { slug: 'long-seam-insp', title: 'Long Seam Inspection', category: 'operator', dataEntryType: 'Barcode-LongSeamInsp' },
  { slug: 'fitup', title: 'Fitup', category: 'operator', dataEntryType: 'Fitup' },
  { slug: 'fitup-queue', title: 'Fitup Queue', category: 'operator', dataEntryType: 'MatQueue-Fitup' },
  { slug: 'round-seam', title: 'Round Seam', category: 'operator', dataEntryType: 'Barcode-RoundSeam' },
  { slug: 'round-seam-insp', title: 'Round Seam Inspection', category: 'operator', dataEntryType: 'Barcode-RoundSeamInsp' },
  { slug: 'rt-xray-queue', title: 'RT X-ray Queue', category: 'operator', dataEntryType: 'RealTimeXray' },
  { slug: 'spot-xray', title: 'Spot X-ray', category: 'operator', dataEntryType: 'Spot' },
  { slug: 'nameplate', title: 'Nameplate (Data Plate)', category: 'operator', dataEntryType: 'DataPlate' },
  { slug: 'hydro', title: 'Hydrostatic Test', category: 'operator', dataEntryType: 'Hydro' },

  // ── Admin / Supervisor Screens ──
  { slug: 'products', title: 'Product Maintenance', category: 'admin', routeMatch: '/menu/products' },
  { slug: 'users', title: 'User Maintenance', category: 'admin', routeMatch: '/menu/users' },
  { slug: 'vendors', title: 'Vendor Maintenance', category: 'admin', routeMatch: '/menu/vendors' },
  { slug: 'work-centers', title: 'Work Center Config', category: 'admin', routeMatch: '/menu/workcenters' },
  { slug: 'defect-codes', title: 'Defect Codes', category: 'admin', routeMatch: '/menu/defect-codes' },
  { slug: 'defect-locations', title: 'Defect Locations', category: 'admin', routeMatch: '/menu/defect-locations' },
  { slug: 'assets', title: 'Asset Management', category: 'admin', routeMatch: '/menu/assets' },
  { slug: 'kanban-cards', title: 'Kanban Card Management', category: 'admin', routeMatch: '/menu/kanban-cards' },
  { slug: 'characteristics', title: 'Characteristics', category: 'admin', routeMatch: '/menu/characteristics' },
  { slug: 'control-plans', title: 'Control Plans', category: 'admin', routeMatch: '/menu/control-plans' },
  { slug: 'plant-gear', title: 'Plant Gear', category: 'admin', routeMatch: '/menu/plant-gear' },
  { slug: 'whos-on-floor', title: "Who's On the Floor", category: 'admin', routeMatch: '/menu/whos-on-floor' },
  { slug: 'production-lines', title: 'Production Lines', category: 'admin', routeMatch: '/menu/production-lines' },
  { slug: 'annotation-types', title: 'Annotation Types', category: 'admin', routeMatch: '/menu/annotation-types' },
  { slug: 'annotations', title: 'Annotation Maintenance', category: 'admin', routeMatch: '/menu/annotations' },
  { slug: 'serial-lookup', title: 'Serial Number Lookup', category: 'admin', routeMatch: '/menu/serial-lookup' },
  { slug: 'sellable-tank-status', title: 'Sellable Tank Status', category: 'admin', routeMatch: '/menu/sellable-tank-status' },
  { slug: 'plant-printers', title: 'Plant Printers', category: 'admin', routeMatch: '/menu/plant-printers' },
  { slug: 'report-issue', title: 'Report Issue', category: 'admin', routeMatch: '/menu/report-issue' },
  { slug: 'issue-approvals', title: 'Issue Approvals', category: 'admin', routeMatch: '/menu/issue-approvals' },
  { slug: 'ai-review', title: 'AI Review', category: 'admin', routeMatch: '/menu/ai-review' },
  { slug: 'production-logs', title: 'Log Viewer', category: 'admin', routeMatch: '/menu/production-logs' },
  { slug: 'supervisor-dashboard', title: 'Supervisor / Team Lead Dashboard', category: 'admin', routeMatch: '/menu/supervisor-dashboard' },
  { slug: 'downtime-reasons', title: 'Downtime Reasons', category: 'admin', routeMatch: '/menu/downtime-reasons' },
  { slug: 'digital-twin', title: 'Digital Twin', category: 'admin', routeMatch: '/menu/digital-twin' },
  { slug: 'shift-schedule', title: 'Shift Schedule', category: 'admin', routeMatch: '/menu/shift-schedule' },
  { slug: 'capacity-targets', title: 'Capacity Targets', category: 'admin', routeMatch: '/menu/capacity-targets' },
];

export const categoryLabels: Record<HelpArticle['category'], string> = {
  general: 'General',
  operator: 'Operator Screens',
  admin: 'Admin & Supervisor',
};

const articlesBySlug = new Map(helpArticles.map((a) => [a.slug, a]));

export function getArticleBySlug(slug: string): HelpArticle | undefined {
  return articlesBySlug.get(slug);
}

export function getArticlesForCategory(category: HelpArticle['category']): HelpArticle[] {
  return helpArticles.filter((a) => a.category === category);
}
