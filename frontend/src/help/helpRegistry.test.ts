import { describe, it, expect } from 'vitest';
import {
  helpArticles,
  getArticleBySlug,
  getArticlesForCategory,
  categoryLabels,
} from './helpRegistry';

describe('helpRegistry', () => {
  it('contains articles for all three categories', () => {
    const categories = new Set(helpArticles.map((a) => a.category));
    expect(categories).toEqual(new Set(['general', 'operator', 'admin']));
  });

  it('has unique slugs', () => {
    const slugs = helpArticles.map((a) => a.slug);
    expect(new Set(slugs).size).toBe(slugs.length);
  });

  it('getArticleBySlug returns the correct article', () => {
    const article = getArticleBySlug('rolls');
    expect(article).toBeDefined();
    expect(article!.title).toBe('Rolls');
    expect(article!.category).toBe('operator');
    expect(article!.dataEntryType).toBe('Rolls');
  });

  it('getArticleBySlug returns undefined for unknown slug', () => {
    expect(getArticleBySlug('nonexistent')).toBeUndefined();
  });

  it('getArticlesForCategory returns only matching articles', () => {
    const general = getArticlesForCategory('general');
    expect(general.length).toBeGreaterThan(0);
    expect(general.every((a) => a.category === 'general')).toBe(true);
  });

  it('operator articles have dataEntryType or routeMatch', () => {
    const operator = getArticlesForCategory('operator');
    for (const a of operator) {
      const hasMapping = a.dataEntryType != null || a.routeMatch != null;
      expect(hasMapping).toBe(true);
    }
  });

  it('admin articles all have routeMatch', () => {
    const admin = getArticlesForCategory('admin');
    for (const a of admin) {
      expect(a.routeMatch).toBeDefined();
    }
  });

  it('categoryLabels has entries for all categories', () => {
    expect(categoryLabels.general).toBe('General');
    expect(categoryLabels.operator).toBe('Operator Screens');
    expect(categoryLabels.admin).toBe('Admin & Supervisor');
  });

  it('includes help mappings for newer admin routes', () => {
    const byRoute = new Map(
      helpArticles
        .filter((a) => a.routeMatch)
        .map((a) => [a.routeMatch, a.slug]),
    );

    expect(byRoute.get('/menu/checklists')).toBe('checklists');
    expect(byRoute.get('/menu/downtime-events')).toBe('downtime-events');
    expect(byRoute.get('/menu/frontend-telemetry')).toBe('frontend-telemetry');
    expect(byRoute.get('/menu/test-coverage')).toBe('test-coverage');
  });
});
