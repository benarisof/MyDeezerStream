import { HttpInterceptorFn } from '@angular/common/http';

export const logInterceptor: HttpInterceptorFn = (req, next) => {
  console.log('🌐 URL:', req.url);
  // On liste les headers pour voir si 'authorization' est là
  const headers: any = {};
  req.headers.keys().forEach(key => headers[key] = req.headers.get(key));
  console.log('🔑 Headers détaillés :', headers);
  
  return next(req);
};