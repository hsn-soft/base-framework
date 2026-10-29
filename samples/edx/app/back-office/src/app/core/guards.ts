import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { SessionService } from './session.service';

/** A page of the signed-in only. The session (menu and permissions) is loaded before the page is. */
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const session = inject(SessionService);
  const router = inject(Router);
  if (!auth.isSignedIn()) {
    return router.parseUrl('/login');
  }
  if (!session.loaded()) {
    try {
      await session.load();
    } catch {
      auth.logout();
      return false;
    }
  }
  return true;
};

/**
 * A page that is a menu item: it opens only when the user's menu has it, which is when the user has its visibility permission. An address typed by hand to a page the user may not see goes to the forbidden page, as the menu does not show it.
 * The code of the item is the `menu` of the route.
 */
export const menuGuard: CanActivateFn = (route) => {
  const session = inject(SessionService);
  const router = inject(Router);
  const code = route.data['menu'] as string;
  return session.leaf(code) ? true : router.parseUrl('/forbidden');
};
