import { Routes } from '@angular/router';
import { adminGuard, userGuard } from './guards/auth.guard';
import { LandingComponent } from './components/landing/landing.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';

export const routes: Routes = [
  { path: '',         component: LandingComponent },
  { path: 'login',    component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes)
  },
  {
    path: 'user',
    canActivate: [userGuard],
    loadChildren: () => import('./features/user/user.routes').then(m => m.userRoutes)
  },
  { path: '**', redirectTo: '' }
];

