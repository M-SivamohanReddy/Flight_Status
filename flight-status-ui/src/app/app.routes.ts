import { Routes } from '@angular/router';
import { adminGuard, userGuard } from './guards/auth.guard';
import { LandingComponent } from './components/landing/landing.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { AdminDashboardComponent } from './components/admin-dashboard/admin-dashboard.component';
import { UserDashboardComponent } from './components/user-dashboard/user-dashboard.component';

export const routes: Routes = [
  { path: '',          component: LandingComponent },
  { path: 'login',     component: LoginComponent },
  { path: 'register',  component: RegisterComponent },
  { path: 'admin/flights', component: AdminDashboardComponent, canActivate: [adminGuard] },
  { path: 'user',          component: UserDashboardComponent,  canActivate: [userGuard]  },
  { path: '**', redirectTo: '' }
];
