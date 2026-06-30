import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

// Sends HttpOnly auth cookie on API calls
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.startsWith(environment.apiUrl)) {
    req = req.clone({ withCredentials: true });
  }

  return next(req);
};
