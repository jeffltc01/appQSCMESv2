import { useLocation } from 'react-router-dom';
import { helpArticles, type HelpArticle } from './helpRegistry.ts';

export function useCurrentHelpArticle(dataEntryType?: string): HelpArticle {
  const { pathname } = useLocation();

  if (pathname.startsWith('/operator') && dataEntryType) {
    const match = helpArticles.find((a) => a.dataEntryType === dataEntryType);
    if (match) return match;
    return helpArticles.find((a) => a.slug === 'operator-layout')!;
  }

  const routeSorted = [...helpArticles]
    .filter((a) => a.routeMatch)
    .sort((a, b) => b.routeMatch!.length - a.routeMatch!.length);

  for (const article of routeSorted) {
    if (pathname === article.routeMatch || pathname.startsWith(article.routeMatch! + '/')) {
      return article;
    }
  }

  return helpArticles[0];
}
